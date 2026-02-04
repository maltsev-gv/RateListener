using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        Timer.Interval = TimeSpan.FromMinutes(1);
        Timer.Tick += (sender, e) => _ = FetchAllRatesAsync();
        Timer.Start();
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

    public static event Action RatesUpdated;

    public static bool IsReceiving { get; private set; }

    private static readonly DispatcherTimer Timer = new();

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
            var dt = DateTime.Now;
            Debug.WriteLine("Start getting rates");
            await BankProvider.SupportedBankProviders.ForEachAsync(async bankProvider =>
            {
                RatesResponse ratesResponse;
                var ratesProvider = bankProvider.RatesProvider;

                try
                {
                    ratesResponse = await ratesProvider.GetRatesResponse();
                    ratesResponse.Success = true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error getting rates: {ex}");
                    Debug.WriteLine($"Error getting rates: {ex}");
                    ratesResponse = new RatesResponse()
                    {
                        Success = false,
                        Message = ex.ToString()
                    };
                }

                CacheHelper.StoreResponse(ratesProvider, ratesResponse);
            });
            Debug.WriteLine($"Rates are updated in {(DateTime.Now - dt).TotalMilliseconds} ms");

            RatesUpdated?.Invoke();
        }
        finally
        {
            IsReceiving = false;
        }
    }
}