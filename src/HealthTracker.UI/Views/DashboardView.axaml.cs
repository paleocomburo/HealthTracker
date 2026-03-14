namespace HealthTracker.UI.Views;

using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using HealthTracker.UI.ViewModels;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;

public partial class DashboardView : UserControl
{
    private DashboardViewModel? _vm;
    private Text? _weightHoverText;
    private Text? _bpHoverText;
    private Text? _bsHoverText;
    private string _weightHoverLabel = "";
    private string _bpHoverLabel = "";
    private string _bsHoverLabel = "";

    public DashboardView()
    {
        InitializeComponent();

        // DataContextChanged is more reliable than AttachedToVisualTree here because
        // Avalonia 11 may propagate DataContext after the visual tree is attached.
        DataContextChanged += async (_, _) =>
        {
            if (_vm is not null)
                _vm.PropertyChanged -= OnVmPropertyChanged;

            _vm = DataContext as DashboardViewModel;

            if (_vm is not null)
            {
                _vm.PropertyChanged += OnVmPropertyChanged;
                await _vm.LoadAllCommand.ExecuteAsync(null);
            }
        };

        WeightPlot.PointerMoved += (_, e) => OnPlotPointerMoved(e, WeightPlot, ref _weightHoverText, ref _weightHoverLabel,
            (p, pl) => _vm?.GetWeightHoverLabel(p, pl));
        BpPlot.PointerMoved += (_, e) => OnPlotPointerMoved(e, BpPlot, ref _bpHoverText, ref _bpHoverLabel,
            (p, pl) => _vm?.GetBloodPressureHoverLabel(p, pl));
        BsPlot.PointerMoved += (_, e) => OnPlotPointerMoved(e, BsPlot, ref _bsHoverText, ref _bsHoverLabel,
            (p, pl) => _vm?.GetBloodSugarHoverLabel(p, pl));
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DashboardViewModel.PlotVersion) || _vm is null) return;

        _vm.ConfigureWeightPlot(WeightPlot.Plot);
        _weightHoverText = CreateHoverText(WeightPlot.Plot);
        _weightHoverLabel = "";
        WeightPlot.Refresh();

        _vm.ConfigureBloodPressurePlot(BpPlot.Plot);
        _bpHoverText = CreateHoverText(BpPlot.Plot);
        _bpHoverLabel = "";
        BpPlot.Refresh();

        _vm.ConfigureBloodSugarPlot(BsPlot.Plot);
        _bsHoverText = CreateHoverText(BsPlot.Plot);
        _bsHoverLabel = "";
        BsPlot.Refresh();
    }

    private static Text CreateHoverText(Plot plot)
    {
        var text = plot.Add.Text("", new Coordinates(0, 0));
        text.LabelStyle.Alignment = Alignment.MiddleRight;
        text.LabelStyle.FontSize = 12;
        text.LabelStyle.OffsetX = -8;
        text.LabelStyle.BackgroundColor = new ScottPlot.Color(255, 255, 200, 220);
        text.LabelStyle.BorderColor = new ScottPlot.Color(180, 180, 100, 180);
        text.LabelStyle.BorderWidth = 1;
        text.LabelStyle.Padding = 4;
        text.IsVisible = false;
        return text;
    }

    private static void OnPlotPointerMoved(
        PointerEventArgs e,
        AvaPlot plotControl,
        ref Text? hoverText,
        ref string hoverLabel,
        Func<Pixel, Plot, (string Label, Coordinates Location)?> getHit)
    {
        if (hoverText is null) return;

        var pos = e.GetPosition(plotControl);
        var pixel = new Pixel((float)pos.X, (float)pos.Y);
        var hit = getHit(pixel, plotControl.Plot);

        var wasVisible = hoverText.IsVisible;
        hoverText.IsVisible = hit is not null;

        if (hit is { } h)
        {
            hoverText.LabelStyle.Text = h.Label;
            hoverText.Location = h.Location;
        }

        // Only re-render when something actually changed to avoid thrashing
        if (hoverText.IsVisible != wasVisible || (hit?.Label ?? "") != hoverLabel)
        {
            hoverLabel = hit?.Label ?? "";
            plotControl.Refresh();
        }
    }
}
