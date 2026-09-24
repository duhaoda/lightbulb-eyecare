using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LightBulb.Core;
using LightBulb.PlatformInterop;
using PowerKit;
using PowerKit.Extensions;

namespace LightBulb.Services;

public partial class GammaService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly IDisposable _eventSubscription;

    private bool _isUpdatingGamma;

    private IReadOnlyList<DeviceContext> _deviceContexts = [];
    private bool _areDeviceContextsValid;
    private DateTimeOffset _lastGammaInvalidationTimestamp = DateTimeOffset.MinValue;

    private ColorConfiguration? _lastConfiguration;
    private double _lastSaturation = 1.0;
    private DateTimeOffset _lastUpdateTimestamp = DateTimeOffset.MinValue;

    public GammaService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        // Listen to all system events that may indicate that the device context or gamma was changed from the outside
        _eventSubscription = Disposable.Merge(
            // 饱和度变化立刻生效（不等平滑过渡的下一周期）——2026-09-22 定制
            settingsService.WatchProperty(
                o => o.ColorSaturation,
                _ => MagnificationInterop.SetSaturation(settingsService.ColorSaturation)
            ),
            // https://github.com/Tyrrrz/LightBulb/issues/223
            SystemHook.TryRegister(SystemHook.Ids.ForegroundWindowChanged, InvalidateGamma)
                ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.ConsoleDisplayStateChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.PowerSavingStatusChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.SessionDisplayStatusChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.MonitorPowerStateChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.AwayModeChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            SystemEvent.Register(SystemEvent.Ids.DisplayChanged, InvalidateDeviceContexts),
            SystemEvent.Register(SystemEvent.Ids.PaletteChanged, InvalidateDeviceContexts),
            SystemEvent.Register(SystemEvent.Ids.SettingsChanged, InvalidateDeviceContexts),
            SystemEvent.Register(SystemEvent.Ids.SystemColorsChanged, InvalidateDeviceContexts)
        );
    }

    private void EnsureValidDeviceContexts()
    {
        if (_areDeviceContextsValid)
            return;

        _areDeviceContextsValid = true;

        Disposable.Merge(_deviceContexts).Dispose();
        _deviceContexts = Monitor
            .GetAll()
            .Select(m => m.TryCreateDeviceContext())
            .WhereNotNull()
            .ToArray();

        _lastConfiguration = null;
    }

    private bool IsGammaStale()
    {
        var instant = DateTimeOffset.Now;

        // Assume gamma continues to be stale for some time after it has been invalidated.
        // This needs to be reasonably long because some external overrides (e.g. Windows
        // applying its own gamma ramp when the Quick Settings panel is opened for the
        // first time) don't happen immediately after the triggering event, but shortly
        // after it -- so we need to keep re-checking for a while to catch and correct them.
        // https://github.com/Tyrrrz/LightBulb/issues/448
        if ((instant - _lastGammaInvalidationTimestamp).Duration() <= TimeSpan.FromSeconds(2))
        {
            // Avoid spamming gamma updates on frequent invalidation sources (e.g. foreground window changes).
            return (instant - _lastUpdateTimestamp).Duration() >= TimeSpan.FromMilliseconds(200);
        }

        // If polling is enabled, assume gamma is stale after some time has passed since the last update
        if (
            _settingsService.IsGammaPollingEnabled
            && (instant - _lastUpdateTimestamp).Duration() > TimeSpan.FromSeconds(1)
        )
        {
            return true;
        }

        return false;
    }

    private bool IsSignificantChange(ColorConfiguration configuration)
    {
        // Nothing to compare to
        if (_lastConfiguration is not { } lastConfiguration)
            return true;

        return Math.Abs(configuration.Temperature - lastConfiguration.Temperature) > 15
            || Math.Abs(configuration.Brightness - lastConfiguration.Brightness) > 0.01
            || Math.Abs(_settingsService.ColorSaturation - _lastSaturation) > 0.01;
    }

    public void InvalidateGamma()
    {
        // Don't invalidate gamma when we're in the process of changing it ourselves,
        // to avoid an infinite loop.
        if (_isUpdatingGamma)
            return;

        _lastGammaInvalidationTimestamp = DateTimeOffset.Now;
        Debug.WriteLine("Gamma invalidated.");
    }

    public void InvalidateDeviceContexts()
    {
        _areDeviceContextsValid = false;
        Debug.WriteLine("Device contexts invalidated.");

        InvalidateGamma();
    }

    public void SetGamma(ColorConfiguration configuration)
    {
        // Avoid unnecessary changes as updating too often will cause stuttering
        if (!IsGammaStale() && !IsSignificantChange(configuration))
            return;

        EnsureValidDeviceContexts();

        _isUpdatingGamma = true;

        foreach (var deviceContext in _deviceContexts)
        {
            var (r, g, b) = ApplySaturation(
                GetRed(configuration),
                GetGreen(configuration),
                GetBlue(configuration),
                configuration.Saturation
            );

            deviceContext.SetGamma(
                r * configuration.Brightness,
                g * configuration.Brightness,
                b * configuration.Brightness
            );
        }

        _isUpdatingGamma = false;

        // 饱和度用全局设置（Magnification 全屏颜色矩阵）。gamma 表只逐通道映射，做不到通道混合。
        MagnificationInterop.SetSaturation(_settingsService.ColorSaturation);

        _lastConfiguration = configuration;
        _lastSaturation = _settingsService.ColorSaturation;
        _lastUpdateTimestamp = DateTimeOffset.Now;
        Debug.WriteLine($"Updated gamma to {configuration}.");
    }

    public void Dispose()
    {
        // Reset gamma on all contexts
        foreach (var deviceContext in _deviceContexts)
            deviceContext.ResetGamma();

        // 同时清掉全屏颜色矩阵，退出后不留灰度残留
        MagnificationInterop.Reset();
        MagnificationInterop.Uninitialize();

        _eventSubscription.Dispose();
        Disposable.Merge(_deviceContexts).Dispose();
    }
}

public partial class GammaService
{
    private static double GetRed(ColorConfiguration configuration)
    {
        // Algorithm taken from http://tannerhelland.com/4435/convert-temperature-rgb-algorithm-code

        if (configuration.Temperature > 6600)
        {
            return (
                Math.Pow(configuration.Temperature / 100 - 60, -0.1332047592) * 329.698727446 / 255
            ).Clamp(0, 1);
        }

        return 1;
    }

    private static double GetGreen(ColorConfiguration configuration)
    {
        // Algorithm taken from http://tannerhelland.com/4435/convert-temperature-rgb-algorithm-code

        if (configuration.Temperature > 6600)
        {
            return (
                Math.Pow(configuration.Temperature / 100 - 60, -0.0755148492) * 288.1221695283 / 255
            ).Clamp(0, 1);
        }

        return (
            (Math.Log(configuration.Temperature / 100) * 99.4708025861 - 161.1195681661) / 255
        ).Clamp(0, 1);
    }

    private static double GetBlue(ColorConfiguration configuration)
    {
        // Algorithm taken from http://tannerhelland.com/4435/convert-temperature-rgb-algorithm-code

        if (configuration.Temperature >= 6600)
            return 1;

        if (configuration.Temperature <= 1900)
            return 0;

        return (
            (Math.Log(configuration.Temperature / 100 - 10) * 138.5177312231 - 305.0447927307) / 255
        ).Clamp(0, 1);
    }

    /// <summary>
    /// 按 Rec.601 亮度权重做去饱和：饱和度 0 = 整屏真灰度。
    /// 在硬件 gamma LUT 层生效（三通道查找表趋同即为灰度），不是软件滤镜。
    /// </summary>
    private static (double R, double G, double B) ApplySaturation(
        double r,
        double g,
        double b,
        double saturation
    )
    {
        var s = Math.Clamp(saturation, 0, 1);
        if (s >= 1)
            return (r, g, b);

        var lum = 0.299 * r + 0.587 * g + 0.114 * b;
        return (lum + (r - lum) * s, lum + (g - lum) * s, lum + (b - lum) * s);
    }
}
