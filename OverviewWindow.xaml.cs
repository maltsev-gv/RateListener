using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using RateListener.Helpers;
using RateListener.ViewModels;

namespace RateListener;

public partial class OverviewWindow
{
    public OverviewWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
                Hide();
        };
        Loaded += (_, _) => AutostartCheckBox.IsChecked = AutoStartHelper.IsEnabled();
    }

    private void OpenHistory_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ListenerSettingsViewModel vm)
            new RateHistoryWindow(vm) { Owner = this }.ShowDialog();
    }

    private void AutostartCheckBox_OnChecked(object sender, RoutedEventArgs e) =>
        AutoStartHelper.SetEnabled(true);

    private void AutostartCheckBox_OnUnchecked(object sender, RoutedEventArgs e) =>
        AutoStartHelper.SetEnabled(false);

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!App.IsReallyExiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }
}
