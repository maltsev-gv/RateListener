using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using RateListener.Helpers;
using RateListener.Service;

namespace RateListener.ViewModels;

public class OverviewViewModel : ViewModelBase
{
    public OverviewViewModel()
    {
        AddCommand = new RelayCommand(AddListener);
        DeleteCommand = new RelayCommand(DeleteListener);
        Task.Run(LoadConfig);
    }

    private static void LoadConfig()
    {
        try
        {
            var settings = ConfigHelper.LoadSettings();
            if (settings != null)
            {
                RunInMainThread(() =>
                {
                    Listeners.Clear();
                    ConfigHelper.IsLoading = true;
                    settings.ForEach(si => Listeners.Add(si.ToViewModel()));
                    ConfigHelper.IsLoading = false;
                });
            }
        }
        catch (Exception e)
        {
            var message = $"Loading configuration failed: {e.Message}"; 
            Logger.Log(message);
            // RunInMainThread(() =>
            //     MessageBox.Show(message, "Error", MessageBoxButton.OK));
        }
    }

    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }

    public static ObservableCollection<ListenerSettingsViewModel> Listeners { get; } = [];

    public ListenerSettingsViewModel SelectedListener 
    {
        get => GetVal<ListenerSettingsViewModel>();
        set => SetVal(value);
    }

    private void AddListener(object obj)
    {
        var listener = new ListenerSettingsViewModel();
        Listeners.Add(listener);
        SelectedListener = listener;
    }

    private void DeleteListener(object obj)
    {
        if (SelectedListener != null)
        {
            Listeners.Remove(SelectedListener);
            SelectedListener = Listeners.LastOrDefault();
        }
    }
}