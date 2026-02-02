using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using RateListener.Helpers;
using RateListener.Service;
using RateListener.Models;
using RateListener.ExtensionMethods;
using System.Windows.Threading;

namespace RateListener.ViewModels;

public class OverviewViewModel : ViewModelBase
{
    public OverviewViewModel()
    {
        AddCommand = new RelayCommand(AddListener);
        DeleteCommand = new RelayCommand(DeleteListener);
        Task.Run(LoadConfig);
    }
    
    static OverviewViewModel()
    {
        timer.Interval = TimeSpan.FromMinutes(1);
        timer.Tick += (sender, e) => _ = FetchAllRatesAsync();
        timer.Start();
    }
    
    private static void LoadConfig()
    {
        try
        {
            var settings = ConfigHelper.LoadSettings();
            if (settings != null)
            {
                RunInMainThread(void () =>
                {
                    Listeners.Clear();
                    ConfigHelper.IsLoading = true;
                    settings.ForEach(si => Listeners.Add(si.ToViewModel()));
                    ConfigHelper.IsLoading = false;
                    
                    _ = FetchAllRatesAsync();
                });
            }
        }
        catch (Exception e)
        {
            var message = $"Loading configuration failed: {e.Message}"; 
            Logger.Log(message);
        }
    }

    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }

    public static ObservableCollection<ListenerSettingsViewModel> Listeners { get; } = [];

    public static event Action? RatesUpdated;

    public static bool IsReceiving { get; private set; }

    private static readonly DispatcherTimer timer = new();

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

    public static async Task FetchAllRatesAsync()
    {
        if (IsReceiving)
            return;

        IsReceiving = true;

        try
        {
            await BankProvider.SupportedBankProviders.ForEachAsync(async bankProvider =>
            {
                RatesResponse ratesResponse;
                var ratesProvider = bankProvider.RatesProvider;

                var cachedResponse = CacheHelper.GetCachedResponse(ratesProvider);
                try
                {
                    ratesResponse = cachedResponse 
                                    ?? await ratesProvider.GetRatesResponse();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error getting rates: {ex}");
                    return;
                }

                CacheHelper.StoreResponse(ratesProvider, ratesResponse);
            });

            RatesUpdated?.Invoke();
        }
        finally
        {
            IsReceiving = false;
        }
    }
}