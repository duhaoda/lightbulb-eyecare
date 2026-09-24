using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightBulb.Framework;
using LightBulb.Localization;
using LightBulb.PlatformInterop;
using LightBulb.Services;
using LightBulb.Utils.Extensions;
using LightBulb.ViewModels.Components;
using LightBulb.ViewModels.Components.Settings;
using PowerKit.Extensions;
using Process = System.Diagnostics.Process;

namespace LightBulb.ViewModels;

public partial class MainViewModel(
    DashboardViewModel dashboard,
    ViewModelManager viewModelManager,
    DialogManager dialogManager,
    SettingsService settingsService,
    LocalizationManager localizationManager,
    UpdateService updateService,
    BreakReminderService breakReminderService
) : ViewModelBase
{
    private readonly Timer _checkForUpdatesTimer = new(
        TimeSpan.FromHours(3),
        async () =>
        {
            try
            {
                var updateVersion = await updateService.CheckForUpdatesAsync();
                if (updateVersion is not null)
                    await updateService.PrepareUpdateAsync(updateVersion);
            }
            catch
            {
                // Failure to update shouldn't crash the application
            }
        }
    );

    private bool _isInitialized;

    public LocalizationManager LocalizationManager { get; } = localizationManager;

    public DashboardViewModel Dashboard { get; } = dashboard;

    public BreakReminderService BreakReminderService { get; } = breakReminderService;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    /// <summary>窗口是否处于全屏（2026-09-22 定制）：全屏时主界面两侧显示状态信息。</summary>
    [ObservableProperty]
    public partial bool IsFullScreen { get; set; }

    private async Task FinalizePendingUpdateAsync()
    {
        var updateVersion = updateService.TryGetLastPreparedUpdate();
        if (updateVersion is null)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.UpdateAvailableTitle,
            string.Format(LocalizationManager.UpdateAvailableMessage, Program.Name, updateVersion),
            LocalizationManager.InstallButton,
            LocalizationManager.CancelButton
        );

        // Bring the user's attention to this dialog, even if the app is hidden
        Application.Current?.ApplicationLifetime?.TryGetMainWindow()?.ShowActivateFocus();

        if (await dialogManager.ShowDialogAsync(dialog) != true)
            return;

        updateService.FinalizeUpdate(updateVersion);

        App.Shutdown(2);
    }

    private async Task ShowDevelopmentBuildMessageAsync()
    {
        if (!Program.IsDevelopmentBuild)
            return;

        // If debugging, the user is likely a developer
        if (Debugger.IsAttached)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.UnstableBuildTitle,
            string.Format(LocalizationManager.UnstableBuildMessage, Program.Name),
            LocalizationManager.SeeReleasesButton,
            LocalizationManager.CloseButton
        );

        if (await dialogManager.ShowDialogAsync(dialog) == true)
            Process.StartShellExecute(Program.ProjectReleasesUrl);
    }

    private async Task ShowGammaRangeMessageAsync()
    {
        if (settingsService.IsExtendedGammaRangeUnlocked)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.LimitedGammaRangeTitle,
            string.Format(LocalizationManager.LimitedGammaRangeMessage, Program.Name),
            LocalizationManager.FixButton,
            LocalizationManager.CloseButton
        );

        if (await dialogManager.ShowDialogAsync(dialog) != true)
            return;

        settingsService.IsExtendedGammaRangeUnlocked = true;
        settingsService.Save();
    }

    private async Task ShowFirstTimeExperienceMessageAsync()
    {
        if (!settingsService.IsFirstTimeExperienceEnabled)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.WelcomeTitle,
            string.Format(LocalizationManager.WelcomeMessage, Program.Name),
            LocalizationManager.OkButton,
            LocalizationManager.CloseButton
        );

        // Disable this message in the future
        settingsService.IsFirstTimeExperienceEnabled = false;
        settingsService.IsAutoStartEnabled = true;
        settingsService.Save();

        if (await dialogManager.ShowDialogAsync(dialog) != true)
            return;

        var settingsDialog = viewModelManager.GetSettingsViewModel();
        settingsDialog.ActivateTab<LocationSettingsTabViewModel>();

        await dialogManager.ShowDialogAsync(settingsDialog);
    }

    public override async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        await FinalizePendingUpdateAsync();

        await ShowDevelopmentBuildMessageAsync();
        await ShowGammaRangeMessageAsync();
        await ShowFirstTimeExperienceMessageAsync();

        _checkForUpdatesTimer.Start();
    }

    [RelayCommand]
    private async Task ShowSettingsAsync() =>
        await dialogManager.ShowDialogAsync(viewModelManager.GetSettingsViewModel());

    [RelayCommand]
    private void ShowAbout() => Process.StartShellExecute(Program.ProjectUrl);

    [RelayCommand]
    private void TakeBreak() => BreakReminderService.StartBreak();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _checkForUpdatesTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
