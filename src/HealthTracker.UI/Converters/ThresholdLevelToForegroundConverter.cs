namespace HealthTracker.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HealthTracker.Shared.Enums;

/// <summary>
/// Maps a ThresholdLevel to the foreground text colour used for the average value in tables.
/// </summary>
public class ThresholdLevelToForegroundConverter : IValueConverter
{
    public static readonly ThresholdLevelToForegroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ThresholdLevel level
            ? level switch
            {
                ThresholdLevel.Warning    => new SolidColorBrush(Color.Parse("#F59E0B")), // amber
                ThresholdLevel.Danger     => new SolidColorBrush(Color.Parse("#EF4444")), // red
                ThresholdLevel.BelowLower => new SolidColorBrush(Color.Parse("#60A5FA")), // blue
                _                         => new SolidColorBrush(Color.Parse("#4ADE80"))  // green for normal
            }
            : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
