namespace HealthTracker.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
/// Returns true when the count is less than 3 — used to enable the "Add Reading" button for blood sugar.
/// </summary>
public class CountLessThanThreeConverter : IValueConverter
{
    public static readonly CountLessThanThreeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count < 3;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
