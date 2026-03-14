namespace HealthTracker.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
/// Returns true when the bound integer count is greater than zero.
/// Used to show/hide the notification bar.
/// </summary>
public class CountToBoolConverter : IValueConverter
{
    public static readonly CountToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
