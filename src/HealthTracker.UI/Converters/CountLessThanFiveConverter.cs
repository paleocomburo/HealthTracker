namespace HealthTracker.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
/// Returns true when the count is less than 5 — used to enable the "Add Reading" button for blood pressure.
/// </summary>
public class CountLessThanFiveConverter : IValueConverter
{
    public static readonly CountLessThanFiveConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count < 5;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
