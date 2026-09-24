using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LightBulb.PlatformInterop;
using PowerKit;
using PowerKit.Extensions;

namespace LightBulb.Services;

/// <summary>
/// Tracks how long the user has been working continuously and requests a break once
/// the configured work interval elapses. Mirrors the break reminder found in CareUEyes,
/// including the "20-20-20" rule (every 20 minutes, look at something 20 feet away for 20 seconds).
/// </summary>
public partial class BreakReminderService : ObservableObject, IDisposable
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private readonly SettingsService _settingsService;
    private readonly Timer _timer;
    private readonly IDisposable _eventSubscription;

    private TimeSpan _elapsedSinceLastBreak;

    public BreakReminderService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        _timer = new Timer(TickInterval, () => Dispatcher.UIThread.Post(Tick));

        // Restart the cycle whenever the user changes the work interval
        _eventSubscription = Disposable.Merge(
            settingsService.WatchProperty(o => o.BreakWorkDuration, _ => Reset())
        );

        _timer.Start();
    }

    /// <summary>
    /// Raised when a break has become due and the reminder should be shown.
    /// </summary>
    public event EventHandler? BreakStarted;

    /// <summary>
    /// Raised when the break is over (either naturally or because it was skipped).
    /// </summary>
    public event EventHandler? BreakEnded;

    public TimeSpan WorkDuration =>
        _settingsService.BreakWorkDuration < TimeSpan.FromMinutes(1)
            ? TimeSpan.FromMinutes(1)
            : _settingsService.BreakWorkDuration;

    public TimeSpan BreakDuration =>
        _settingsService.BreakDuration < TimeSpan.FromSeconds(5)
            ? TimeSpan.FromSeconds(5)
            : _settingsService.BreakDuration;

    [ObservableProperty]
    public partial bool IsBreakInProgress { get; set; }

    [ObservableProperty]
    public partial TimeSpan TimeUntilNextBreak { get; set; }

    [ObservableProperty]
    public partial TimeSpan RemainingBreakTime { get; set; }

    private void Tick()
    {
        if (!_settingsService.IsBreakReminderEnabled)
        {
            // Keep the timer idle while the reminder is disabled
            if (IsBreakInProgress)
                EndBreak();

            _elapsedSinceLastBreak = TimeSpan.Zero;
            TimeUntilNextBreak = WorkDuration;
            return;
        }

        if (IsBreakInProgress)
        {
            RemainingBreakTime -= TickInterval;
            if (RemainingBreakTime <= TimeSpan.Zero)
                EndBreak();

            return;
        }

        _elapsedSinceLastBreak += TickInterval;

        if (_elapsedSinceLastBreak >= WorkDuration)
        {
            StartBreak();
        }
        else
        {
            TimeUntilNextBreak = WorkDuration - _elapsedSinceLastBreak;
        }
    }

    public void StartBreak()
    {
        if (IsBreakInProgress)
            return;

        _elapsedSinceLastBreak = TimeSpan.Zero;
        TimeUntilNextBreak = WorkDuration;
        RemainingBreakTime = BreakDuration;
        IsBreakInProgress = true;

        BreakStarted?.Invoke(this, EventArgs.Empty);
    }

    public void EndBreak()
    {
        if (!IsBreakInProgress)
            return;

        IsBreakInProgress = false;
        RemainingBreakTime = TimeSpan.Zero;
        Reset();

        BreakEnded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Restarts the work interval from scratch.
    /// </summary>
    public void Reset()
    {
        _elapsedSinceLastBreak = TimeSpan.Zero;
        TimeUntilNextBreak = WorkDuration;
    }

    /// <summary>
    /// Ends the current break (if any) and schedules the next reminder to occur
    /// after the specified duration.
    /// </summary>
    public void PostponeBreak(TimeSpan duration)
    {
        if (IsBreakInProgress)
            EndBreak();

        _elapsedSinceLastBreak = WorkDuration - duration;
        if (_elapsedSinceLastBreak < TimeSpan.Zero)
            _elapsedSinceLastBreak = TimeSpan.Zero;

        TimeUntilNextBreak = WorkDuration - _elapsedSinceLastBreak;
    }

    public void Dispose()
    {
        _eventSubscription.Dispose();
        _timer.Dispose();
    }
}
