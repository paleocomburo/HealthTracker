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
using HealthTracker.UI.ViewModels.BloodSugar;
using HealthTracker.UI.ViewModels.Shared;
using HealthTracker.UI.Views.BloodSugar;
using HealthTracker.UI.Models;
using ScottPlot;
using Serilog;

public partial class BloodSugarViewModel(
    BloodSugarService bsService,
    SettingsService settingsService,
    IDialogService dialogService,
    CsvExportService csvExportService,
    NotificationViewModel notifications) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<BloodSugarEntryDisplay> _entries = [];
    [ObservableProperty] private int _plotVersion;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public DateRangeFilterViewModel Filter { get; } = new();

    private AppSettings _currentSettings = AppSettings.Default;
    private IReadOnlyList<BloodSugarEntry> _plotData = [];
    private List<BloodSugarEntry> _orderedPlotData = [];
    private ScottPlot.Plottables.Scatter? _scatter;

    public BloodSugarViewModel Init()
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
            var data = await bsService.GetEntries(Filter.RangeStart, Filter.RangeEnd, ct);

            var displays = data
                .OrderByDescending(e => e.Date)
                .Select(e =>
                {
                    var avg = e.Readings.Average();
                    var level = ThresholdEvaluator.EvaluateBloodSugar(avg, _currentSettings.Thresholds);
                    return new BloodSugarEntryDisplay(e, avg, level);
                })
                .ToList();

            Dispatcher.UIThread.Post(() =>
            {
                Entries = new ObservableCollection<BloodSugarEntryDisplay>(displays);
                _plotData = data;
                PlotVersion++;
            });
        }
        catch (DataFileCorruptException ex)
        {
            Log.Error(ex, "Blood sugar data file corrupt: {FilePath}", ex.FilePath);
            ErrorMessage = $"Data file is corrupt: {ex.FilePath}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load blood sugar entries");
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
        var vm = new BloodSugarEntryDialogViewModel();
        var dialog = new BloodSugarEntryDialog(vm);
        await ShowDialog(dialog);

        var result = await vm.Result;
        if (result is null) return;

        try
        {
            var saved = await bsService.AddEntry(result.Date, result.Readings);
            CheckThresholds(saved);
            await LoadEntries();
        }
        catch (ValidationException ex)
        {
            notifications.Post(ex.Message, NotificationSeverity.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add blood sugar entry");
        }
    }

    [RelayCommand]
    private async Task EditEntry(BloodSugarEntryDisplay display)
    {
        var entry = display.Entry;
        var vm = new BloodSugarEntryDialogViewModel
        {
            ExistingId = entry.Id,
            EntryDate = new DateTimeOffset(entry.Date.ToDateTime(TimeOnly.MinValue))
        };

        for (var i = 1; i < entry.Readings.Count; i++)
            vm.AddReadingCommand.Execute(null);

        for (var i = 0; i < entry.Readings.Count && i < vm.Readings.Count; i++)
            vm.Readings[i].ReadingInput = entry.Readings[i].ToString(System.Globalization.CultureInfo.CurrentCulture);

        var dialog = new BloodSugarEntryDialog(vm);
        await ShowDialog(dialog);

        var result = await vm.Result;
        if (result is null) return;

        try
        {
            await bsService.UpdateEntry(result);
            CheckThresholds(result);
            await LoadEntries();
        }
        catch (ValidationException ex)
        {
            notifications.Post(ex.Message, NotificationSeverity.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update blood sugar entry");
        }
    }

    [RelayCommand]
    private async Task DeleteEntry(BloodSugarEntryDisplay display)
    {
        var entry = display.Entry;
        var confirmed = await dialogService.ShowConfirmation(
            "Delete Entry",
            $"Delete blood sugar entry for {entry.Date:MMM d, yyyy}?");

        if (!confirmed) return;

        try
        {
            await bsService.DeleteEntry(entry.Id, entry.Date);
            await LoadEntries();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete blood sugar entry");
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        var filePath = await dialogService.ShowSaveFileDialog(
            $"bloodsugar_{Filter.RangeStart:yyyy-MM-dd}_{Filter.RangeEnd:yyyy-MM-dd}.csv",
            "CSV Files", ".csv");

        if (filePath is null) return;

        try
        {
            await csvExportService.ExportBloodSugar(Filter.RangeStart, Filter.RangeEnd, filePath);
            notifications.Post($"Exported to {filePath}", NotificationSeverity.Info);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to export blood sugar data");
            notifications.Post("Export failed. See logs for details.", NotificationSeverity.Warning);
        }
    }

    private void CheckThresholds(BloodSugarEntry entry)
    {
        var avg = entry.Readings.Average();
        var level = ThresholdEvaluator.EvaluateBloodSugar(avg, _currentSettings.Thresholds);

        if (level != ThresholdLevel.None)
        {
            var msg = level switch
            {
                ThresholdLevel.Danger     => $"Blood sugar average {avg:F1} mmol/L is at danger level.",
                ThresholdLevel.BelowLower => $"Blood sugar average {avg:F1} mmol/L is below the lower limit.",
                _                         => $"Blood sugar average {avg:F1} mmol/L exceeds the warning level."
            };
            notifications.Post(msg, NotificationSeverity.Warning);
        }
    }

    public void ConfigurePlot(Plot plot)
    {
        plot.Clear();
        PlotTheme.Apply(plot);
        plot.Title("Blood Sugar");
        plot.YLabel("mmol/L");

        if (_plotData.Count > 0)
        {
            _orderedPlotData = _plotData.OrderBy(x => x.Date).ToList();
            var xs = _orderedPlotData.Select(e => e.Date.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
            var ys = _orderedPlotData.Select(e => (double)e.Readings.Average()).ToArray();

            _scatter = plot.Add.Scatter(xs, ys);
            _scatter.Color = Colors.MediumPurple;
            _scatter.MarkerSize = 5;
            _scatter.LegendText = "Avg (mmol/L)";
        }
        else
        {
            _orderedPlotData = [];
            _scatter = null;
        }

        var t = _currentSettings.Thresholds;

        if (t.BloodSugarWarningMmolL is { } warn)
        {
            var line = plot.Add.HorizontalLine((double)warn);
            line.Color = Colors.Orange;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"Warning: {warn} mmol/L";
        }

        if (t.BloodSugarDangerMmolL is { } danger)
        {
            var line = plot.Add.HorizontalLine((double)danger);
            line.Color = Colors.Tomato;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"Danger: {danger} mmol/L";
        }

        if (t.BloodSugarLowerMmolL is { } lower)
        {
            var line = plot.Add.HorizontalLine((double)lower);
            line.Color = Colors.SteelBlue;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"Lower: {lower} mmol/L";
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
        if (_scatter is null || _orderedPlotData.Count == 0) return null;

        var coords = plot.GetCoordinates(mousePixel);
        var nearest = _scatter.Data.GetNearest(coords, plot.LastRender);
        if (!nearest.IsReal || nearest.Index >= _orderedPlotData.Count) return null;

        var entry = _orderedPlotData[nearest.Index];
        var avg = entry.Readings.Average();
        var label = $"{entry.Date:MMM d, yyyy}\n{avg:F1} mmol/L";
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

public record BloodSugarEntryDisplay(
    BloodSugarEntry Entry,
    decimal AvgReading,
    ThresholdLevel Level)
{
    public bool HasMultipleReadings => Entry.Readings.Count > 1;
    public IReadOnlyList<string> ReadingDisplays =>
        Entry.Readings.Select(r => $"{r:F1} mmol/L").ToList();
    public string ReadingsLabel => Entry.Readings.Count switch
    {
        1 => "single reading",
        _ => $"avg of {Entry.Readings.Count}"
    };
    public string? StatusLabel => Level switch
    {
        ThresholdLevel.Danger     => "Too high",
        ThresholdLevel.Warning    => "Warning",
        ThresholdLevel.BelowLower => "Below lower",
        _                         => null
    };
    public bool HasStatus => Level != ThresholdLevel.None;
}
