namespace HealthTracker.UI.Models;

using ScottPlot;

/// <summary>
/// Applies the app's dark colour palette to a ScottPlot Plot so all charts
/// are visually consistent with the rest of the UI.
/// </summary>
public static class PlotTheme
{
    // App palette colours expressed as R,G,B bytes
    private static readonly Color FigureBg    = new(0x1A, 0x1A, 0x2E); // matches window bg
    private static readonly Color DataBg      = new(0x22, 0x22, 0x38); // slightly lighter than window bg
    private static readonly Color AxisColor   = new(0x90, 0x90, 0xB8); // muted axis/tick labels
    private static readonly Color GridColor   = new(0x2A, 0x2A, 0x48); // subtle grid lines
    private static readonly Color LegendBg    = new(0x20, 0x20, 0x38);
    private static readonly Color LegendBorder = new(0x40, 0x40, 0x68);
    private static readonly Color LegendText  = new(0xC0, 0xC0, 0xD8);

    public static void Apply(Plot plot)
    {
        plot.FigureBackground.Color = FigureBg;
        plot.DataBackground.Color   = DataBg;
        plot.Axes.Color(AxisColor);

        plot.Grid.MajorLineColor = GridColor;
        plot.Grid.MinorLineColor = GridColor.WithAlpha(60);

        plot.Legend.BackgroundColor = LegendBg;
        plot.Legend.OutlineColor    = LegendBorder;
        plot.Legend.FontColor       = LegendText;
    }

    /// <summary>
    /// Re-applies axis colours after a call to <c>Axes.DateTimeTicksBottom()</c>, which
    /// installs a new bottom axis that resets tick label colours back to the ScottPlot
    /// default (black). Call this once after <c>DateTimeTicksBottom()</c>.
    /// </summary>
    public static void ReapplyAxisColors(Plot plot) => plot.Axes.Color(AxisColor);
}
