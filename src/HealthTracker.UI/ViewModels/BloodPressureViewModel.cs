namespace HealthTracker.UI.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthTracker.Services;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Enums;
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;
using HealthTracker.UI.ViewModels.BloodPressure;
using HealthTracker.UI.ViewModels.Shared;
using HealthTracker.UI.Views.BloodPressure;
using HealthTracker.UI.Models;
using ScottPlot;
using Serilog;

public partial class BloodPressureViewModel(
    BloodPressureService bpService,
    SettingsService settingsService,
    IDialogService dialogService,
    CsvExportService csvExportService,
    NotificationViewModel notifications) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<BloodPressureEntryDisplay> _entries = [];
    [ObservableProperty] private int _plotVersion;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public DateRangeFilterViewModel Filter { get; } = new();

    private AppSettings _currentSettings = AppSettings.Default;
    private IReadOnlyList<BloodPressureEntry> _plotData = [];
    private List<BloodPressureEntry> _orderedPlotData = [];
    private ScottPlot.Plottables.Scatter? _systolicScatter;
    private ScottPlot.Plottables.Scatter? _diastolicScatter;

    public BloodPressureViewModel Init()
    {
        Filter.RangeChanged += async (_, _) => await LoadEntries();
        return this;
    }

    [RelayCommand]
    public async Task LoadEntries(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _currentSettings = await settingsService.Load(ct);
            var data = await bpService.GetEntries(Filter.RangeStart, Filter.RangeEnd, ct);

            var displays = data
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.TimeOfDay)
                .Select(e => new BloodPressureEntryDisplay(
                    e,
                    e.Readings.Average(r => r.SystolicMmhg),
                    e.Readings.Average(r => r.DiastolicMmhg)))
                .ToList();

            Dispatcher.UIThread.Post(() =>
            {
                Entries = new ObservableCollection<BloodPressureEntryDisplay>(displays);
                _plotData = data;
                PlotVersion++;
            });
        }
        catch (DataFileCorruptException ex)
        {
            Log.Error(ex, "BP data file corrupt: {FilePath}", ex.FilePath);
            ErrorMessage = $"Data file is corrupt: {ex.FilePath}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load blood pressure entries");
            ErrorMessage = "Failed to load data. See logs for details.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddEntry()
    {
        var vm = new BloodPressureEntryDialogViewModel();
        var dialog = new BloodPressureEntryDialog(vm);
        await ShowDialog(dialog);

        var result = await vm.Result;
        if (result is null) return;

        try
        {
            var saved = await bpService.AddEntry(result.Date, result.TimeOfDay, result.Readings);
            CheckThresholds(saved);
            await LoadEntries();
        }
        catch (ValidationException ex)
        {
            notifications.Post(ex.Message, NotificationSeverity.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add blood pressure entry");
        }
    }

    [RelayCommand]
    private async Task EditEntry(BloodPressureEntryDisplay display)
    {
        var entry = display.Entry;
        var vm = new BloodPressureEntryDialogViewModel
        {
            ExistingId = entry.Id,
            EntryDate = new DateTimeOffset(entry.Date.ToDateTime(TimeOnly.MinValue)),
            TimeOfDay = entry.TimeOfDay
        };

        foreach (var r in entry.Readings)
        {
            if (vm.Readings.Count < entry.Readings.Count)
                vm.AddReadingCommand.Execute(null);
        }

        for (var i = 0; i < entry.Readings.Count && i < vm.Readings.Count; i++)
        {
            vm.Readings[i].SystolicInput = entry.Readings[i].SystolicMmhg.ToString();
            vm.Readings[i].DiastolicInput = entry.Readings[i].DiastolicMmhg.ToString();
        }

        var dialog = new BloodPressureEntryDialog(vm);
        await ShowDialog(dialog);

        var result = await vm.Result;
        if (result is null) return;

        try
        {
            await bpService.UpdateEntry(result);
            CheckThresholds(result);
            await LoadEntries();
        }
        catch (ValidationException ex)
        {
            notifications.Post(ex.Message, NotificationSeverity.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update blood pressure entry");
        }
    }

    [RelayCommand]
    private async Task DeleteEntry(BloodPressureEntryDisplay display)
    {
        var entry = display.Entry;
        var confirmed = await dialogService.ShowConfirmation(
            "Delete Entry",
            $"Delete blood pressure entry for {entry.Date:MMM d, yyyy} ({entry.TimeOfDay})?");

        if (!confirmed) return;

        try
        {
            await bpService.DeleteEntry(entry.Id, entry.Date);
            await LoadEntries();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete blood pressure entry");
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        var filePath = await dialogService.ShowSaveFileDialog(
            $"bloodpressure_{Filter.RangeStart:yyyy-MM-dd}_{Filter.RangeEnd:yyyy-MM-dd}.csv",
            "CSV Files", ".csv");

        if (filePath is null) return;

        try
        {
            await csvExportService.ExportBloodPressure(Filter.RangeStart, Filter.RangeEnd, filePath);
            notifications.Post($"Exported to {filePath}", NotificationSeverity.Info);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export blood pressure data");
            notifications.Post("Export failed. See logs for details.", NotificationSeverity.Warning);
        }
    }

    private void CheckThresholds(BloodPressureEntry entry)
    {
        var avgSys = (int)Math.Round(entry.Readings.Average(r => r.SystolicMmhg));
        var avgDia = (int)Math.Round(entry.Readings.Average(r => r.DiastolicMmhg));
        var level = ThresholdEvaluator.EvaluateBloodPressure(avgSys, avgDia, _currentSettings.Thresholds);

        if (level != ThresholdLevel.None)
            notifications.Post(
                $"Blood pressure {avgSys}/{avgDia} mmHg exceeds your configured threshold.",
                NotificationSeverity.Warning);
    }

    public void ConfigurePlot(Plot plot)
    {
        plot.Clear();
        PlotTheme.Apply(plot);
        plot.Title("Blood Pressure");
        plot.YLabel("mmHg");

        if (_plotData.Count > 0)
        {
            _orderedPlotData = _plotData.OrderBy(x => x.Date).ToList();
            var xs = _orderedPlotData.Select(e => e.Date.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();

            _systolicScatter = plot.Add.Scatter(
                xs,
                _orderedPlotData.Select(e => e.Readings.Average(r => r.SystolicMmhg)).ToArray());
            _systolicScatter.Color = Colors.Tomato;
            _systolicScatter.MarkerSize = 5;
            _systolicScatter.LegendText = "Systolic";

            _diastolicScatter = plot.Add.Scatter(
                xs,
                _orderedPlotData.Select(e => e.Readings.Average(r => r.DiastolicMmhg)).ToArray());
            _diastolicScatter.Color = Colors.SteelBlue;
            _diastolicScatter.MarkerSize = 5;
            _diastolicScatter.LegendText = "Diastolic";
        }
        else
        {
            _orderedPlotData = [];
            _systolicScatter = null;
            _diastolicScatter = null;
        }

        var t = _currentSettings.Thresholds;

        if (t.BpSystolicUpperMmhg is { } sys)
        {
            var line = plot.Add.HorizontalLine(sys);
            line.Color = Colors.Tomato;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"Systolic limit: {sys}";
        }

        if (t.BpDiastolicUpperMmhg is { } dia)
        {
            var line = plot.Add.HorizontalLine(dia);
            line.Color = Colors.SteelBlue;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"Diastolic limit: {dia}";
        }

        // Explicitly set the X-axis to the selected filter range so the chart always
        // covers the chosen period regardless of whether there are data points in it.
        var xMin = Filter.RangeStart.ToDateTime(TimeOnly.MinValue).ToOADate();
        var xMax = Filter.RangeEnd.ToDateTime(TimeOnly.MinValue).ToOADate();
        plot.Axes.DateTimeTicksBottom();
        PlotTheme.ReapplyAxisColors(plot);
        plot.Axes.SetLimitsX(xMin, xMax);
        plot.ShowLegend();
    }

    public (string Label, ScottPlot.Coordinates Location)? GetHoverLabel(ScottPlot.Pixel mousePixel, Plot plot)
    {
        if ((_systolicScatter is null && _diastolicScatter is null) || _orderedPlotData.Count == 0) return null;

        var coords = plot.GetCoordinates(mousePixel);

        // Both series share identical X values, so nearest.Index refers to the same entry
        // regardless of which series we query. We check systolic first and fall back to
        // diastolic so hovering either series' data points triggers the tooltip.
        var nearestSys = _systolicScatter?.Data.GetNearest(coords, plot.LastRender) ?? default;
        var nearestDia = _diastolicScatter?.Data.GetNearest(coords, plot.LastRender) ?? default;
        var nearest = nearestSys.IsReal ? nearestSys : nearestDia;

        if (!nearest.IsReal || nearest.Index >= _orderedPlotData.Count) return null;

        var entry = _orderedPlotData[nearest.Index];
        var sys = entry.Readings.Average(r => r.SystolicMmhg);
        var dia = entry.Readings.Average(r => r.DiastolicMmhg);
        var label = $"{entry.Date:MMM d, yyyy} ({entry.TimeOfDay})\nSys: {sys:F1} / Dia: {dia:F1} mmHg";
        if (entry.Readings.Count > 1)
            label += $"\n({entry.Readings.Count} readings averaged)";
        return (label, new ScottPlot.Coordinates(nearest.X, nearest.Y));
    }

    private static async Task ShowDialog(Avalonia.Controls.Window dialog)
    {
        var mainWindow = (Avalonia.Application.Current?.ApplicationLifetime as
            IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (mainWindow is not null)
            await dialog.ShowDialog(mainWindow);
        else
            dialog.Show();
    }
}

public record BloodPressureEntryDisplay(
    BloodPressureEntry Entry,
    double AvgSystolic,
    double AvgDiastolic)
{
    public bool HasMultipleReadings => Entry.Readings.Count > 1;
    public string AvgDisplay => $"{AvgSystolic:F1} / {AvgDiastolic:F1}";
    public IReadOnlyList<string> ReadingDisplays =>
        Entry.Readings.Select(r => $"{r.SystolicMmhg} / {r.DiastolicMmhg}").ToList();
}
