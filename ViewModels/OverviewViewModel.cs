using System;
using System.Collections.Generic;
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
        Timer.Interval = TimeSpan.FromSeconds(15);
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

                    _ = FetchAllRatesAsync(true);
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
            SelectedListener.Unsubscribe();
            Listeners.Remove(SelectedListener);
            SelectedListener = Listeners.LastOrDefault();
        }
    }

    public static async Task FetchAllRatesAsync(bool force = false)
    {
        if (IsReceiving)
            return;

        IsReceiving = true;

        try
        {
            var dt = DateTime.Now;
            Debug.WriteLine("Start getting rates");
            var now = DateTime.Now;
            var dueProviders = BankProvider.SupportedBankProviders
                .Where(bankProvider => force || IsDueForUpdate(bankProvider, now))
                .ToArray();
            if (dueProviders.Length == 0)
                return;
            await dueProviders.ForEachAsync(async bankProvider =>
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

                lock (LastFetchLock)
                {
                    LastFetchByProvider[bankProvider.Name] = DateTime.Now;
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

    private static readonly object LastFetchLock = new();
    private static readonly Dictionary<string, DateTime> LastFetchByProvider = [];

    private static bool IsDueForUpdate(BankProvider bankProvider, DateTime now)
    {
        lock (LastFetchLock)
        {
            var last = LastFetchByProvider.GetValueOrDefault(bankProvider.Name, DateTime.MinValue);
            return (now - last).TotalSeconds >= EffectiveIntervalSec(bankProvider.Name);
        }
    }

    private static double EffectiveIntervalSec(string providerName)
    {
        var intervals = Listeners
            .Where(l => l.BankProviderName == providerName)
            .Select(l => l.PollIntervalSec > 0
                ? l.PollIntervalSec
                : BankProvider.DefaultPollIntervalSec(providerName))
            .ToList();
        return intervals.Count > 0
            ? intervals.Min()
            : BankProvider.DefaultPollIntervalSec(providerName);
    }
}