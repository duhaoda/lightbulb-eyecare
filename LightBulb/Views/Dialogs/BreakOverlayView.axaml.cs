using System;
using Avalonia.Threading;
using LightBulb.Framework;
using LightBulb.ViewModels.Dialogs;

namespace LightBulb.Views.Dialogs;

public partial class BreakOverlayView : Window<BreakOverlayViewModel>
{
    private readonly DispatcherTimer _topmostTimer;

    public BreakOverlayView()
    {
        InitializeComponent();

        // Windows drops the topmost flag as soon as another topmost window (the Start
        // menu, other always-on-top tools) is activated, and it does not come back on its
        // own. Re-asserting it periodically keeps the overlay on top for the whole break.
        // Focus is only taken once, so this doesn't fight the user for the keyboard.
        _topmostTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(2),
            DispatcherPriority.Normal,
            (_, _) => ReassertTopmost()
        );

        Opened += (_, _) =>
        {
            // A borderless window often ignores the state that was assigned in XAML
            // before the window was mapped, which used to leave the overlay invisible.
            if (WindowState != Avalonia.Controls.WindowState.Maximized)
                WindowState = Avalonia.Controls.WindowState.Maximized;

            ReassertTopmost();
            Activate();

            _topmostTimer.Start();
        };

        Closed += (_, _) => _topmostTimer.Stop();
    }

    private void ReassertTopmost()
    {
        // Toggling the flag is what actually re-applies it on Windows
        Topmost = false;
        Topmost = true;
    }
}
