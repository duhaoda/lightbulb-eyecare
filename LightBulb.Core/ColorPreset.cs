using System.Collections.Generic;

namespace LightBulb.Core;

/// <summary>
/// A named color configuration that can be applied with a single click,
/// similar to the preset modes offered by CareUEyes.
/// </summary>
public readonly record struct ColorPreset(
    string Id,
    double Temperature,
    double Brightness,
    double Saturation = 1.0
);

public static class ColorPresets
{
    // Temperature and brightness values are chosen to roughly match the
    // preset modes offered by CareUEyes.
    // 阅读：饱和度归零 → 整屏真灰度，像纸质书（2026-09-22 定制）
    public static ColorPreset Reading { get; } = new("Reading", 5200, 0.85, 0.0);

    public static ColorPreset Office { get; } = new("Office", 6600, 0.95);

    public static ColorPreset Night { get; } = new("Night", 3400, 0.6);

    public static ColorPreset Movie { get; } = new("Movie", 6000, 0.8);

    public static ColorPreset Coding { get; } = new("Coding", 5000, 0.88);

    public static ColorPreset Game { get; } = new("Game", 7200, 1);

    public static ColorPreset EyeCare { get; } = new("EyeCare", 4600, 0.75);

    public static ColorPreset Custom { get; } = new("Custom", 5000, 0.8);

    public static IReadOnlyList<ColorPreset> All { get; } =
    [Reading, Office, Night, Movie, Coding, Game, EyeCare, Custom];

    public static bool TryGet(string? id, out ColorPreset preset)
    {
        foreach (var candidate in All)
        {
            if (candidate.Id == id)
            {
                preset = candidate;
                return true;
            }
        }

        preset = default;
        return false;
    }
}
