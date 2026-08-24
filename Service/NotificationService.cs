using System.Linq;
using System.Media;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using RateListener.ViewModels;

namespace RateListener.Service;

public static class NotificationService
{
    private const string TrayIconResourceKey = "TrayIcon";

    private static ListenerSettingsViewModel lastAlertedListener;

    private static TaskbarIcon TrayIcon =>
        (TaskbarIcon)Application.Current.Resources[TrayIconResourceKey];

    public static void ShowRateAlert(ListenerSettingsViewModel listener,
        string lastOptimum, string optimum, string changeType)
    {
        lastAlertedListener = listener;
        SystemSounds.Exclamation.Play();
        TrayIcon.ShowBalloonTip("Rate listener",
            $"New optimum found: {optimum} instead of {lastOptimum} ({changeType})",
            BalloonIcon.Info);
    }

    public static void ActivateMainWindow()
    {
        var window = Application.Current.MainWindow;
        if (window == null)
            return;
        if (lastAlertedListener != null && window.DataContext is OverviewViewModel overviewViewModel)
            overviewViewModel.SelectedListener = OverviewViewModel.Listeners
                .FirstOrDefault(l => l.Id == lastAlertedListener.Id);
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }
}
