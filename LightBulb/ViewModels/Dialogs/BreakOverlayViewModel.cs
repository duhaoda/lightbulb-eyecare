using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightBulb.Framework;
using LightBulb.Localization;
using LightBulb.Services;

namespace LightBulb.ViewModels.Dialogs;

public partial class BreakOverlayViewModel : ViewModelBase
{
    public BreakReminderService BreakReminderService { get; }

    public LocalizationManager LocalizationManager { get; }

    /// <summary>本次弹出要显示的护眼文案（每次休息开始时随机重抽一条）。</summary>
    [ObservableProperty]
    public partial string CurrentMessage { get; set; }

    public BreakOverlayViewModel(
        BreakReminderService breakReminderService,
        LocalizationManager localizationManager
    )
    {
        BreakReminderService = breakReminderService;
        LocalizationManager = localizationManager;

        // 文案在"每次弹出"时重抽，而不是只在 VM 构造时抽一次
        // （VM 由 DI 持有，构造可能只发生一次）
        CurrentMessage = BreakMessages.PickRandom();
        breakReminderService.BreakStarted += (_, _) => CurrentMessage = BreakMessages.PickRandom();
    }

    [RelayCommand]
    private void SkipBreak() => BreakReminderService.EndBreak();

    [RelayCommand]
    private void PostponeBreak() => BreakReminderService.PostponeBreak(TimeSpan.FromMinutes(5));
}
