using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using RateListener.Helpers;
using RateListener.Service;
using RateListener.ViewModels;

namespace RateListener;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static bool IsReallyExiting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        if (Resources["TrayIcon"] is TaskbarIcon trayIcon)
            InitAutostartMenuItem(trayIcon);

        var startInBackground = e.Args.Any(arg =>
            arg.Equals("/background", StringComparison.OrdinalIgnoreCase));
        MainWindow = new OverviewWindow();
        if (!startInBackground)
            MainWindow.Show();
    }

    private static void InitAutostartMenuItem(TaskbarIcon trayIcon)
    {
        var autostartItem = trayIcon.ContextMenu?.Items.OfType<MenuItem>()
            .FirstOrDefault(item => item.Name == "AutostartMenuItem");
        autostartItem?.IsChecked = AutoStartHelper.IsEnabled();
    }

    public void ShutdownApp()
    {
        IsReallyExiting = true;
        if (Resources["TrayIcon"] is TaskbarIcon icon)
            icon.Dispose();
        ConfigHelper.FlushPendingWrites();
        Shutdown();
        Environment.Exit(0);
    }

    private void TrayIcon_OnTrayLeftMouseUp(object sender, RoutedEventArgs e) =>
        NotificationService.ActivateMainWindow();

    private void TrayIcon_OnBalloonTipClicked(object sender, RoutedEventArgs e) =>
        NotificationService.ActivateMainWindow();

    private void OpenClick(object sender, RoutedEventArgs e) =>
        NotificationService.ActivateMainWindow();

    private void UpdateNowClick(object sender, RoutedEventArgs e) =>
        _ = OverviewViewModel.FetchAllRatesAsync(true);

    private void AutostartClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { IsChecked: var isChecked })
            AutoStartHelper.SetEnabled(isChecked);
    }

    private void ExitClick(object sender, RoutedEventArgs e) =>
        ShutdownApp();
}
