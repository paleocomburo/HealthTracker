namespace HealthTracker.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HealthTracker.UI.ViewModels;

/// <summary>
/// Maps a NotificationSeverity to a background brush for the notification banner.
/// </summary>
public class SeverityToBrushConverter : IValueConverter
{
    public static readonly SeverityToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is NotificationSeverity severity
            ? severity switch
            {
                NotificationSeverity.Warning => new SolidColorBrush(Color.Parse("#D97706")),
                NotificationSeverity.Danger  => new SolidColorBrush(Color.Parse("#DC2626")),
                _                            => new SolidColorBrush(Color.Parse("#2563EB"))
            }
            : Brushes.Gray;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
