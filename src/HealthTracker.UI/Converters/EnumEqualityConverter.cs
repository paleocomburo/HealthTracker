namespace HealthTracker.UI.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

/// <summary>
/// One-way converter that checks whether a bound enum value equals the converter parameter.
/// Used to set button highlight states when a mode is active.
/// </summary>
public class EnumEqualityConverter : IValueConverter
{
    public static readonly EnumEqualityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.Equals(parameter) ?? false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
