using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace LightBulb.Converters;

/// <summary>
/// Returns <c>true</c> when the bound preset identifier matches the converter parameter.
/// Used to highlight the active preset mode button.
/// </summary>
public class PresetIdEqualsConverter : IValueConverter
{
    public static PresetIdEqualsConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string id
        && parameter is string expectedId
        && string.Equals(id, expectedId, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
