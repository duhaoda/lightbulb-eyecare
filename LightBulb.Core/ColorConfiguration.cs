using System;
using System.Text.Json.Serialization;

namespace LightBulb.Core;

public readonly partial record struct ColorConfiguration(
    double Temperature,
    double Brightness,
    [property: JsonIgnore] double Saturation = 1.0
)
{
    public ColorConfiguration WithOffset(double temperatureOffset, double brightnessOffset) =>
        new(Temperature + temperatureOffset, Brightness + brightnessOffset, Saturation);

    public ColorConfiguration Clamp(
        double minimumTemperature,
        double maximumTemperature,
        double minimumBrightness,
        double maximumBrightness
    ) =>
        new(
            Math.Clamp(Temperature, minimumTemperature, maximumTemperature),
            Math.Clamp(Brightness, minimumBrightness, maximumBrightness),
            Math.Clamp(Saturation, 0, 1)
        );
}

public partial record struct ColorConfiguration
{
    public static ColorConfiguration Default { get; } = new(6600, 1);
}
