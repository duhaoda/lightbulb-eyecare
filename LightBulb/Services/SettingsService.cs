using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Cogwheel;
using CommunityToolkit.Mvvm.ComponentModel;
using LightBulb.Core;
using LightBulb.Framework;
using LightBulb.Localization;
using LightBulb.Models;
using LightBulb.PlatformInterop;
using Microsoft.Win32;

namespace LightBulb.Services;

[ObservableObject]
public partial class SettingsService()
    : SettingsBase(StartOptions.Current.SettingsPath, SerializerContext.Default)
{
    private readonly RegistrySwitch<int> _extendedGammaRangeSwitch = new(
        RegistryHive.LocalMachine,
        @"Software\Microsoft\Windows NT\CurrentVersion\ICM",
        "GdiICMGammaRange",
        256
    );

    private readonly RegistrySwitch<string> _autoStartSwitch = new(
        RegistryHive.CurrentUser,
        @"Software\Microsoft\Windows\CurrentVersion\Run",
        Program.Name,
        $"\"{Program.ExecutableFilePath}\" {StartOptions.IsInitiallyHiddenArgument}"
    );

    [ObservableProperty]
    public partial bool IsFirstTimeExperienceEnabled { get; set; } = true;

    [ObservableProperty]
    [JsonIgnore] // comes from registry
    public partial bool IsExtendedGammaRangeUnlocked { get; set; }

    // General

    public double MinimumTemperature => 500;

    public double MaximumTemperature => 20_000;

    public double MinimumBrightness => 0.1;

    public double MaximumBrightness => 1;

    // Preset modes

    /// <summary>
    /// Identifier of the preset mode that is currently applied on top of the day/night cycle,
    /// or <c>null</c> when the cycle is not being overridden.
    /// </summary>
    [ObservableProperty]
    public partial string? ActivePresetId { get; set; }

    /// <summary>
    /// 全局颜色饱和度（2026-09-22 定制）。
    /// 1.0 = 原色，0.0 = 全黑白；由 Magnification 的全屏颜色矩阵实现，与色温/亮度互相独立。
    /// 独立成全局项（而不是塞进各个 ColorConfiguration）是为了避免"阅读预设把 Day/Night/Custom
    /// 全部染灰"的污染问题——那会让切到任何档位都是黑白。
    /// </summary>
    [ObservableProperty]
    public partial double ColorSaturation { get; set; } = 1.0;

    /// <summary>
    /// 切换预设时同步饱和度（2026-09-22 定制）：
    /// "阅读"预设 → 黑白（0.0）；其他预设 → 原色（1.0）；切成自动模式 → 原色。
    /// 用户之后仍可在设置里手动微调这个值。
    /// </summary>
    partial void OnActivePresetIdChanged(string? value)
    {
        ColorSaturation =
            value is not null && ColorPresets.TryGet(value, out var preset)
                ? preset.Saturation
                : 1.0;
    }

    /// <summary>
    /// Color configuration used by the customizable "Custom" preset mode.
    /// </summary>
    [ObservableProperty]
    public partial ColorConfiguration CustomPresetConfiguration { get; set; } = new(5000, 0.8);

    [ObservableProperty]
    public partial ColorConfiguration DayConfiguration { get; set; } = new(6600, 1);

    [ObservableProperty]
    public partial ColorConfiguration NightConfiguration { get; set; } = new(3900, 0.85);

    [ObservableProperty]
    public partial TimeSpan ConfigurationTransitionDuration { get; set; } =
        TimeSpan.FromMinutes(40);

    [ObservableProperty]
    public partial double ConfigurationTransitionOffset { get; set; }

    [ObservableProperty]
    public partial TimeSpan ConfigurationSmoothingMaxDuration { get; set; } =
        TimeSpan.FromSeconds(5);

    // Location

    [ObservableProperty]
    public partial bool IsManualSunriseSunsetEnabled { get; set; } = true;

    [ObservableProperty]
    [JsonPropertyName("ManualSunriseTime")]
    public partial TimeOnly ManualSunrise { get; set; } = new(07, 20);

    [ObservableProperty]
    [JsonPropertyName("ManualSunsetTime")]
    public partial TimeOnly ManualSunset { get; set; } = new(16, 30);

    [ObservableProperty]
    public partial GeoLocation? Location { get; set; }

    // Advanced

    [ObservableProperty]
    public partial ThemeVariant Theme { get; set; }

    [ObservableProperty]
    public partial Language Language { get; set; }

    [ObservableProperty]
    [JsonIgnore] // comes from registry
    public partial bool IsAutoStartEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsAutoUpdateEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsDefaultToDayConfigurationEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsConfigurationSmoothingEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsPauseWhenFullScreenEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsGammaPollingEnabled { get; set; }

    // Break reminder

    [ObservableProperty]
    public partial bool IsBreakReminderEnabled { get; set; } = true;

    [ObservableProperty]
    public partial TimeSpan BreakWorkDuration { get; set; } = TimeSpan.FromMinutes(20);

    [ObservableProperty]
    public partial TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(20);

    // Application whitelist

    [ObservableProperty]
    public partial bool IsApplicationWhitelistEnabled { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ExternalApplication>? WhitelistedApplications { get; set; }

    // HotKeys

    [ObservableProperty]
    public partial HotKey ToggleHotKey { get; set; }

    [ObservableProperty]
    [JsonPropertyName("FocusWindowHotKey")]
    public partial HotKey ToggleWindowHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey IncreaseTemperatureOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey DecreaseTemperatureOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey IncreaseBrightnessOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey DecreaseBrightnessOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey ResetConfigurationOffsetHotKey { get; set; }

    public override void Reset()
    {
        base.Reset();

        // Don't reset the first-time experience
        IsFirstTimeExperienceEnabled = false;

        // Trigger UI updates
        OnPropertyChanged(string.Empty);
    }

    public override void Save()
    {
        // Disallow auto-start in debug mode to make things simpler
#if DEBUG
        IsAutoStartEnabled = false;
#endif

        base.Save();

        // Update values in the registry
        try
        {
            _extendedGammaRangeSwitch.IsSet = IsExtendedGammaRangeUnlocked;
            _autoStartSwitch.IsSet = IsAutoStartEnabled;
        }
        catch (Win32Exception)
        {
            // This can happen if the user doesn't have the necessary permissions to update
            // the corresponding registry keys, and privilege elevation has failed.
            // Throwing an exception here is very messy, so we'll just ignore it.
            // https://github.com/Tyrrrz/LightBulb/issues/335
        }

        // Trigger UI updates
        OnPropertyChanged(string.Empty);
    }

    public override bool Load()
    {
        var wasLoaded = base.Load();

        // Get values from the registry
        IsExtendedGammaRangeUnlocked = _extendedGammaRangeSwitch.IsSet;
        IsAutoStartEnabled = _autoStartSwitch.IsSet;

        // Trigger UI updates
        OnPropertyChanged(string.Empty);

        return wasLoaded;
    }
}

public partial class SettingsService
{
    [JsonSerializable(typeof(SettingsService))]
    private partial class SerializerContext : JsonSerializerContext;
}
