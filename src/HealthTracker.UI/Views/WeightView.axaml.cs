namespace HealthTracker.UI.Views;

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using HealthTracker.UI.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;

public partial class WeightView : UserControl
{
    private WeightViewModel? _vm;
    private Text? _hoverText;
    private string _hoverLabel = "";

    public WeightView()
    {
        InitializeComponent();

        DataContextChanged += async (_, _) =>
        {
            if (_vm is not null)
                _vm.PropertyChanged -= OnVmPropertyChanged;

            _vm = DataContext as WeightViewModel;

            if (_vm is not null)
            {
                _vm.PropertyChanged += OnVmPropertyChanged;
                await _vm.LoadEntriesCommand.ExecuteAsync(null);
            }
        };

        PlotControl.PointerMoved += OnPlotPointerMoved;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WeightViewModel.PlotVersion) || _vm is null) return;

        _vm.ConfigurePlot(PlotControl.Plot);
        _hoverText = PlotControl.Plot.Add.Text("", new Coordinates(0, 0));
        _hoverText.LabelStyle.Alignment = Alignment.MiddleRight;
        _hoverText.LabelStyle.FontSize = 12;
        _hoverText.LabelStyle.OffsetX = -8;
        _hoverText.LabelStyle.BackgroundColor = new ScottPlot.Color(255, 255, 200, 220);
        _hoverText.LabelStyle.BorderColor = new ScottPlot.Color(180, 180, 100, 180);
        _hoverText.LabelStyle.BorderWidth = 1;
        _hoverText.LabelStyle.Padding = 4;
        _hoverText.IsVisible = false;
        _hoverLabel = "";
        PlotControl.Refresh();
    }

    private void OnPlotPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_vm is null || _hoverText is null) return;

        var pos = e.GetPosition(PlotControl);
        var pixel = new Pixel((float)pos.X, (float)pos.Y);
        var hit = _vm.GetHoverLabel(pixel, PlotControl.Plot);

        var wasVisible = _hoverText.IsVisible;
        _hoverText.IsVisible = hit is not null;

        if (hit is { } h)
        {
            _hoverText.LabelStyle.Text = h.Label;
            _hoverText.Location = h.Location;
        }

        // Only re-render when something actually changed to avoid thrashing
        if (_hoverText.IsVisible != wasVisible || (hit?.Label ?? "") != _hoverLabel)
        {
            _hoverLabel = hit?.Label ?? "";
            PlotControl.Refresh();
        }
    }
}
