using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using LightBulb.Framework;
using LightBulb.ViewModels;

namespace LightBulb.Views;

public partial class MainView : Window<MainViewModel>
{
    public MainView() => InitializeComponent();

    private void Window_OnOpened(object? sender, EventArgs args) => DataContext.IsOpen = true;

    private void Window_OnClosed(object? sender, EventArgs args) => DataContext.IsOpen = false;

    private void HeaderBorder_OnPointerPressed(object? sender, PointerPressedEventArgs args) =>
        BeginMoveDrag(args);

    private void HideButton_OnClick(object sender, RoutedEventArgs args) =>
        // The window is closed, but the backend and the tray icon will persist
        Close();

    /// <summary>
    /// 全屏 / 退出全屏（2026-09-22 定制）。
    /// 布局在 XAML 里做了居中与宽度上限，全屏后内容不会横向拉伸变形。
    /// </summary>
    private void FullScreenButton_OnClick(object sender, RoutedEventArgs args)
    {
        var goingFullScreen = WindowState != Avalonia.Controls.WindowState.FullScreen;

        WindowState = goingFullScreen
            ? Avalonia.Controls.WindowState.FullScreen
            : Avalonia.Controls.WindowState.Normal;

        // 通知界面切换两侧状态面板的显隐（2026-09-22 定制）
        if (DataContext is MainViewModel vm)
            vm.IsFullScreen = goingFullScreen;
    }
}
