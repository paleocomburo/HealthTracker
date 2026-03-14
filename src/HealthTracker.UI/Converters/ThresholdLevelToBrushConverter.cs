namespace HealthTracker.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HealthTracker.Shared.Enums;

/// <summary>
/// Maps a ThresholdLevel to a cell background brush for table rows.
/// Used in both the blood pressure and blood sugar table views.
/// </summary>
public class ThresholdLevelToBrushConverter : IValueConverter
{
    public static readonly ThresholdLevelToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ThresholdLevel level
            ? level switch
            {
                ThresholdLevel.Warning    => new SolidColorBrush(Color.Parse("#7C4A0A")), // dark amber badge bg
                ThresholdLevel.Danger     => new SolidColorBrush(Color.Parse("#7F1D1D")), // dark red badge bg
                ThresholdLevel.BelowLower => new SolidColorBrush(Color.Parse("#1E3A5F")), // dark blue badge bg
                _                         => null
            }
            : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
