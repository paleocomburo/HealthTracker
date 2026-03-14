namespace HealthTracker.UI.ViewModels;

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
using HealthTracker.Shared.Exceptions;
using HealthTracker.Shared.Interfaces;
using HealthTracker.UI.ViewModels.Shared;
using HealthTracker.UI.ViewModels.Weight;
using HealthTracker.UI.Views.Weight;
using HealthTracker.UI.Models;
using ScottPlot;
using Serilog;

public partial class WeightViewModel(
    WeightService weightService,
    SettingsService settingsService,
    IDialogService dialogService,
    CsvExportService csvExportService,
    NotificationViewModel notifications) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<WeightEntry> _entries = [];
    [ObservableProperty] private int _plotVersion;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public DateRangeFilterViewModel Filter { get; } = new();

    private AppSettings _currentSettings = AppSettings.Default;
    private IReadOnlyList<WeightEntry> _plotData = [];
    private List<WeightEntry> _orderedPlotData = [];
    private ScottPlot.Plottables.Scatter? _scatter;

    public WeightViewModel Init()
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
            var data = await weightService.GetEntries(Filter.RangeStart, Filter.RangeEnd, ct);

            Dispatcher.UIThread.Post(() =>
            {
                Entries = new ObservableCollection<WeightEntry>(
                    data.OrderByDescending(e => e.Date));
                _plotData = data;
                PlotVersion++;
            });
        }
        catch (DataFileCorruptException ex)
        {
            Log.Error(ex, "Weight data file is corrupt: {FilePath}", ex.FilePath);
            ErrorMessage = $"Data file is corrupt: {ex.FilePath}";
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Failed to load weight entries");
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
        var vm = new WeightEntryDialogViewModel();
        var dialog = new WeightEntryDialog(vm);
        await ShowDialog(dialog);

        var result = await vm.Result;
        if (result is null) return;

        try
        {
            await weightService.AddEntry(result.Date, result.WeightKg);
            await LoadEntries();
        }
        catch (ValidationException ex)
        {
            notifications.Post(ex.Message, NotificationSeverity.Warning);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Failed to add weight entry");
        }
    }

    [RelayCommand]
    private async Task EditEntry(WeightEntry entry)
    {
        var vm = new WeightEntryDialogViewModel
        {
            ExistingId = entry.Id,
            EntryDate = new System.DateTimeOffset(entry.Date.ToDateTime(System.TimeOnly.MinValue)),
            WeightKgInput = entry.WeightKg.ToString(System.Globalization.CultureInfo.CurrentCulture)
        };

        var dialog = new WeightEntryDialog(vm);
        await ShowDialog(dialog);

        var result = await vm.Result;
        if (result is null) return;

        try
        {
            await weightService.UpdateEntry(result);
            await LoadEntries();
        }
        catch (ValidationException ex)
        {
            notifications.Post(ex.Message, NotificationSeverity.Warning);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Failed to update weight entry");
        }
    }

    [RelayCommand]
    private async Task DeleteEntry(WeightEntry entry)
    {
        var confirmed = await dialogService.ShowConfirmation(
            "Delete Entry",
            $"Delete weight entry for {entry.Date:MMM d, yyyy}?");

        if (!confirmed) return;

        try
        {
            await weightService.DeleteEntry(entry.Id, entry.Date);
            await LoadEntries();
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Failed to delete weight entry");
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        var filePath = await dialogService.ShowSaveFileDialog(
            $"weight_{Filter.RangeStart:yyyy-MM-dd}_{Filter.RangeEnd:yyyy-MM-dd}.csv",
            "CSV Files", ".csv");

        if (filePath is null) return;

        try
        {
            await csvExportService.ExportWeight(Filter.RangeStart, Filter.RangeEnd, filePath);
            notifications.Post($"Exported to {filePath}", NotificationSeverity.Info);
        }
        catch (System.Exception ex)
        {
            Log.Error(ex, "Failed to export weight data");
            notifications.Post("Export failed. See logs for details.", NotificationSeverity.Warning);
        }
    }

    public void ConfigurePlot(Plot plot)
    {
        plot.Clear();
        PlotTheme.Apply(plot);
        plot.Title("Weight");
        plot.YLabel("kg");

        if (_plotData.Count > 0)
        {
            _orderedPlotData = _plotData.OrderBy(x => x.Date).ToList();
            var xs = _orderedPlotData.Select(e => e.Date.ToDateTime(System.TimeOnly.MinValue).ToOADate()).ToArray();
            var ys = _orderedPlotData.Select(e => (double)e.WeightKg).ToArray();

            _scatter = plot.Add.Scatter(xs, ys);
            _scatter.Color = Colors.SteelBlue;
            _scatter.MarkerSize = 5;
            _scatter.LegendText = "Weight (kg)";
        }
        else
        {
            _orderedPlotData = [];
            _scatter = null;
        }

        // Target weight reference line — only rendered if set in settings.
        if (_currentSettings.TargetWeightKg is { } target)
        {
            var line = plot.Add.HorizontalLine((double)target);
            line.Color = Colors.OrangeRed;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"Target: {target} kg";
        }

        // Explicitly set the X-axis to the selected filter range so the chart always
        // covers the chosen period regardless of whether there are data points in it.
        var xMin = Filter.RangeStart.ToDateTime(System.TimeOnly.MinValue).ToOADate();
        var xMax = Filter.RangeEnd.ToDateTime(System.TimeOnly.MinValue).ToOADate();
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
        return ($"{entry.Date:MMM d, yyyy}\n{entry.WeightKg:F1} kg", new ScottPlot.Coordinates(nearest.X, nearest.Y));
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
