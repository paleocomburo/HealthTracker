namespace HealthTracker.UI.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HealthTracker.Services;
using HealthTracker.Shared.Dtos;
using HealthTracker.Shared.Enums;
using HealthTracker.UI.Messages;
using HealthTracker.UI.ViewModels.Shared;
using HealthTracker.UI.Models;
using ScottPlot;
using Serilog;

public partial class DashboardViewModel(
    WeightService weightService,
    BloodPressureService bpService,
    BloodSugarService bsService,
    SettingsService settingsService) : ViewModelBase
{
    [ObservableProperty] private int _plotVersion;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _weightSummary = "—";
    [ObservableProperty] private string _weightLastReading = "No data";
    [ObservableProperty] private string _bpSummary = "—";
    [ObservableProperty] private string _bpLastReading = "No data";
    [ObservableProperty] private string _bsSummary = "—";
    [ObservableProperty] private string _bsLastReading = "No data";

    public DateRangeFilterViewModel Filter { get; } = new();

    private AppSettings _currentSettings = AppSettings.Default;
    private IReadOnlyList<WeightEntry> _weightData = [];
    private IReadOnlyList<BloodPressureEntry> _bpData = [];
    private IReadOnlyList<BloodSugarEntry> _bsData = [];
    private List<WeightEntry> _orderedWeightData = [];
    private List<BloodPressureEntry> _orderedBpData = [];
    private List<BloodSugarEntry> _orderedBsData = [];
    private ScottPlot.Plottables.Scatter? _weightScatter;
    private ScottPlot.Plottables.Scatter? _bpSystolicScatter;
    private ScottPlot.Plottables.Scatter? _bpDiastolicScatter;
    private ScottPlot.Plottables.Scatter? _bsScatter;

    public DashboardViewModel Init()
    {
        Filter.RangeChanged += async (_, _) => await LoadAll();
        return this;
    }

    [RelayCommand]
    public async Task LoadAll(CancellationToken ct = default)
    {
        IsLoading = true;

        try
        {
            _currentSettings = await settingsService.Load(ct);

            var weightTask = weightService.GetEntries(Filter.RangeStart, Filter.RangeEnd, ct);
            var bpTask = bpService.GetEntries(Filter.RangeStart, Filter.RangeEnd, ct);
            var bsTask = bsService.GetEntries(Filter.RangeStart, Filter.RangeEnd, ct);

            await Task.WhenAll(weightTask, bpTask, bsTask);

            Dispatcher.UIThread.Post(() =>
            {
                _weightData = weightTask.Result;
                _bpData = bpTask.Result;
                _bsData = bsTask.Result;
                UpdateSummaries();
                PlotVersion++;
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load dashboard data");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateSummaries()
    {
        var latestWeight = _weightData.OrderByDescending(e => e.Date).FirstOrDefault();
        WeightSummary = latestWeight is not null ? $"{latestWeight.WeightKg:F1} kg" : "—";
        WeightLastReading = latestWeight is not null ? $"Last reading · {latestWeight.Date:d MMM}" : "No data";

        var latestBp = _bpData.OrderByDescending(e => e.Date).ThenByDescending(e => e.TimeOfDay).FirstOrDefault();
        if (latestBp is not null)
        {
            var sys = (int)Math.Round(latestBp.Readings.Average(r => r.SystolicMmhg));
            var dia = (int)Math.Round(latestBp.Readings.Average(r => r.DiastolicMmhg));
            BpSummary = $"{sys}/{dia} mmHg";
            BpLastReading = $"Last reading · {latestBp.Date:d MMM} ({latestBp.TimeOfDay.ToString().ToLower()})";
        }
        else
        {
            BpSummary = "—";
            BpLastReading = "No data";
        }

        var latestBs = _bsData.OrderByDescending(e => e.Date).FirstOrDefault();
        if (latestBs is not null)
        {
            var avg = latestBs.Readings.Average();
            BsSummary = $"{avg:F1} mmol/L";
            var level = ThresholdEvaluator.EvaluateBloodSugar(avg, _currentSettings.Thresholds);
            var levelStr = level switch
            {
                ThresholdLevel.Danger     => " · above threshold",
                ThresholdLevel.Warning    => " · near threshold",
                ThresholdLevel.BelowLower => " · below limit",
                _                         => ""
            };
            BsLastReading = $"Last reading · {latestBs.Date:d MMM}{levelStr}";
        }
        else
        {
            BsSummary = "—";
            BsLastReading = "No data";
        }
    }

    [RelayCommand]
    private void GoToWeight() =>
        WeakReferenceMessenger.Default.Send(new NavigateMessage("Weight"));

    [RelayCommand]
    private void GoToBloodPressure() =>
        WeakReferenceMessenger.Default.Send(new NavigateMessage("BloodPressure"));

    [RelayCommand]
    private void GoToBloodSugar() =>
        WeakReferenceMessenger.Default.Send(new NavigateMessage("BloodSugar"));

    public void ConfigureWeightPlot(Plot plot)
    {
        plot.Clear();
        PlotTheme.Apply(plot);
        plot.Title("Weight");
        plot.YLabel("kg");

        if (_weightData.Count > 0)
        {
            _orderedWeightData = _weightData.OrderBy(x => x.Date).ToList();
            var xs = _orderedWeightData.Select(e => e.Date.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
            var ys = _orderedWeightData.Select(e => (double)e.WeightKg).ToArray();

            _weightScatter = plot.Add.Scatter(xs, ys);
            _weightScatter.Color = Colors.SteelBlue;
            _weightScatter.MarkerSize = 5;
            _weightScatter.LegendText = "Weight (kg)";
        }
        else
        {
            _orderedWeightData = [];
            _weightScatter = null;
        }

        if (_currentSettings.TargetWeightKg is { } target)
        {
            var line = plot.Add.HorizontalLine((double)target);
            line.Color = Colors.OrangeRed;
            line.LinePattern = LinePattern.Dashed;
            line.LegendText = $"Target: {target} kg";
        }

        var xMin = Filter.RangeStart.ToDateTime(TimeOnly.MinValue).ToOADate();
        var xMax = Filter.RangeEnd.ToDateTime(TimeOnly.MinValue).ToOADate();
        plot.Axes.DateTimeTicksBottom();
        PlotTheme.ReapplyAxisColors(plot);
        plot.Axes.SetLimitsX(xMin, xMax);
        plot.ShowLegend();
    }

    public void ConfigureBloodPressurePlot(Plot plot)
    {
        plot.Clear();
        PlotTheme.Apply(plot);
        plot.Title("Blood Pressure");
        plot.YLabel("mmHg");

        if (_bpData.Count > 0)
        {
            _orderedBpData = _bpData.OrderBy(x => x.Date).ToList();
            var xs = _orderedBpData.Select(e => e.Date.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();

            _bpSystolicScatter = plot.Add.Scatter(
                xs,
                _orderedBpData.Select(e => e.Readings.Average(r => r.SystolicMmhg)).ToArray());
            _bpSystolicScatter.Color = Colors.Tomato;
            _bpSystolicScatter.MarkerSize = 5;
            _bpSystolicScatter.LegendText = "Systolic";

            _bpDiastolicScatter = plot.Add.Scatter(
                xs,
                _orderedBpData.Select(e => e.Readings.Average(r => r.DiastolicMmhg)).ToArray());
            _bpDiastolicScatter.Color = Colors.SteelBlue;
            _bpDiastolicScatter.MarkerSize = 5;
            _bpDiastolicScatter.LegendText = "Diastolic";
        }
        else
        {
            _orderedBpData = [];
            _bpSystolicScatter = null;
            _bpDiastolicScatter = null;
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

        var xMin = Filter.RangeStart.ToDateTime(TimeOnly.MinValue).ToOADate();
        var xMax = Filter.RangeEnd.ToDateTime(TimeOnly.MinValue).ToOADate();
        plot.Axes.DateTimeTicksBottom();
        PlotTheme.ReapplyAxisColors(plot);
        plot.Axes.SetLimitsX(xMin, xMax);
        plot.ShowLegend();
    }

    public void ConfigureBloodSugarPlot(Plot plot)
    {
        plot.Clear();
        PlotTheme.Apply(plot);
        plot.Title("Blood Sugar");
        plot.YLabel("mmol/L");

        if (_bsData.Count > 0)
        {
            _orderedBsData = _bsData.OrderBy(x => x.Date).ToList();
            var xs = _orderedBsData.Select(e => e.Date.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
            var ys = _orderedBsData.Select(e => (double)e.Readings.Average()).ToArray();

            _bsScatter = plot.Add.Scatter(xs, ys);
            _bsScatter.Color = Colors.MediumPurple;
            _bsScatter.MarkerSize = 5;
            _bsScatter.LegendText = "Avg (mmol/L)";
        }
        else
        {
            _orderedBsData = [];
            _bsScatter = null;
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

        var xMin = Filter.RangeStart.ToDateTime(TimeOnly.MinValue).ToOADate();
        var xMax = Filter.RangeEnd.ToDateTime(TimeOnly.MinValue).ToOADate();
        plot.Axes.DateTimeTicksBottom();
        PlotTheme.ReapplyAxisColors(plot);
        plot.Axes.SetLimitsX(xMin, xMax);
        plot.ShowLegend();
    }

    public (string Label, ScottPlot.Coordinates Location)? GetWeightHoverLabel(ScottPlot.Pixel mousePixel, Plot plot)
    {
        if (_weightScatter is null || _orderedWeightData.Count == 0) return null;

        var coords = plot.GetCoordinates(mousePixel);
        var nearest = _weightScatter.Data.GetNearest(coords, plot.LastRender);
        if (!nearest.IsReal || nearest.Index >= _orderedWeightData.Count) return null;

        var entry = _orderedWeightData[nearest.Index];
        return ($"{entry.Date:MMM d, yyyy}\n{entry.WeightKg:F1} kg", new ScottPlot.Coordinates(nearest.X, nearest.Y));
    }

    public (string Label, ScottPlot.Coordinates Location)? GetBloodPressureHoverLabel(ScottPlot.Pixel mousePixel, Plot plot)
    {
        if ((_bpSystolicScatter is null && _bpDiastolicScatter is null) || _orderedBpData.Count == 0) return null;

        var coords = plot.GetCoordinates(mousePixel);
        var nearestSys = _bpSystolicScatter?.Data.GetNearest(coords, plot.LastRender) ?? default;
        var nearestDia = _bpDiastolicScatter?.Data.GetNearest(coords, plot.LastRender) ?? default;
        var nearest = nearestSys.IsReal ? nearestSys : nearestDia;
        if (!nearest.IsReal || nearest.Index >= _orderedBpData.Count) return null;

        var entry = _orderedBpData[nearest.Index];
        var sys = entry.Readings.Average(r => r.SystolicMmhg);
        var dia = entry.Readings.Average(r => r.DiastolicMmhg);
        var label = $"{entry.Date:MMM d, yyyy} ({entry.TimeOfDay})\nSys: {sys:F1} / Dia: {dia:F1} mmHg";
        if (entry.Readings.Count > 1)
            label += $"\n({entry.Readings.Count} readings averaged)";
        return (label, new ScottPlot.Coordinates(nearest.X, nearest.Y));
    }

    public (string Label, ScottPlot.Coordinates Location)? GetBloodSugarHoverLabel(ScottPlot.Pixel mousePixel, Plot plot)
    {
        if (_bsScatter is null || _orderedBsData.Count == 0) return null;

        var coords = plot.GetCoordinates(mousePixel);
        var nearest = _bsScatter.Data.GetNearest(coords, plot.LastRender);
        if (!nearest.IsReal || nearest.Index >= _orderedBsData.Count) return null;

        var entry = _orderedBsData[nearest.Index];
        var avg = entry.Readings.Average();
        var label = $"{entry.Date:MMM d, yyyy}\n{avg:F1} mmol/L";
        if (entry.Readings.Count > 1)
            label += $"\n({entry.Readings.Count} readings averaged)";
        return (label, new ScottPlot.Coordinates(nearest.X, nearest.Y));
    }
}
