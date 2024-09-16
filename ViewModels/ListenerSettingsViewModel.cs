#nullable enable
using RateListener.ExtensionMethods;
using RateListener.Helpers;
using RateListener.Models;
using RateListener.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RateListener.ViewModels
{
    public class ListenerSettingsViewModel : ViewModelBase
    {
        public ListenerSettingsViewModel()
        {
            ExchangeCurrenciesCommand = new RelayCommand(ExchangeCurrencies);
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += _timer_Tick;
            timer.Start();

            StartListening();
        }

        private void ExchangeCurrencies(object obj)
        {
            (SearchToCurr, SearchFromCurr) = (SearchFromCurr, SearchToCurr);
        }

        private void StoreSettings() =>
            ConfigHelper.SaveSettings(this);

        // ReSharper disable once AsyncVoidMethod
        private async void _timer_Tick(object? sender, EventArgs e)
        {
            await UpdateData();
        }

        private async Task UpdateData()
        {
            await ReceiveRatesAsync();
            FindChains();
            RaiseAll();
        }

        private async Task ReceiveRatesAsync()
        {
            if (SelectedBankProvider == null)
            {
                return;
            }

            IsReceiving = true;
            
            RatesResponse ratesResponse;
            var provider = SelectedBankProvider.RatesProvider;

            try
            {
                ratesResponse = await provider.GetRatesResponse();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting rates: {ex.Message}", "Rate listener", MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.OK, options: MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            RunInMainThread(() =>
            {
                Rates.Clear();
                ratesResponse.Data.Mobile.OrderBy(r => r.ToString()).ForEach(c => Rates.Add(c));

                if (Currencies.Count == 0)
                {
                    Rates.SelectMany(r => new[] { r.BuyCode, r.SellCode })
                         .Distinct()
                         .OrderBy(s => s)
                         .ToList()
                         .ForEach(c => Currencies.Add(c));
                }

                FindChains();
                RaiseAll();
            });
            IsReceiving = false;
        }

        private List<Chain> chainList = new();
        private string prevCurrencies = string.Empty;

        private double FindChains(bool getInversedRate = false)
        {
            if (Rates.Count == 0 || SearchFromCurr.IsNullOrEmpty() || SearchToCurr.IsNullOrEmpty() || SearchFromCurr == SearchToCurr)
            {
                return 0;
            }

            bool isCurrChanged = prevCurrencies.IsFilled() && prevCurrencies != $"{SearchFromCurr}_{SearchToCurr}";
            prevCurrencies = $"{SearchFromCurr}_{SearchToCurr}";

            var oldList = chainList.ToList();
            chainList.Clear();
            foreach (var rate in Rates.Where(r => r.IsCurrUsed(getInversedRate ? SearchToCurr : SearchFromCurr)))
            {
                var chain = new List<ChainLink>();
                FindChain(getInversedRate ? SearchToCurr : SearchFromCurr,
                          getInversedRate ? SearchFromCurr : SearchToCurr, 
                          rate, Rates.Where(r => r != rate).ToList(), chain);
                if (chain.LastOrDefault()?.To == (getInversedRate ? SearchFromCurr : SearchToCurr))
                {
                    chainList.Add(new Chain(chain));
                }
            }

            chainList = chainList.OrderByDescending(c => c.EffectiveRate).ToList();
            var bestRate = chainList.FirstOrDefault()?.EffectiveRate ?? 0;

            if (getInversedRate)
            {
                chainList = oldList.ToList();
                return bestRate;
            }

            RunInMainThread(() =>
            {
                Chains.Clear();
                chainList.ForEach(c => Chains.Add(c));
            });

            if (!isCurrChanged && LastEffectiveRate != 0.0)
            {
                if (IsImprovementAlert && bestRate > LastEffectiveRate
                    && (!IsAlertWhenMoreChecked || ParseDouble(AlertWhenMore, out var alertWhenMore) && bestRate > alertWhenMore))
                {
                    ShowNewOptimumWindow(LastEffectiveRate.RateToDisplay(), bestRate.RateToDisplay(), "improved");
                }
                if (IsDepreciationAlert && bestRate < LastEffectiveRate)
                {
                    ShowNewOptimumWindow(LastEffectiveRate.RateToDisplay(), bestRate.RateToDisplay(), "depreciated");
                }
            }
            LastEffectiveRate = bestRate;
            LastUpdateTime = DateTime.Now;
            return bestRate;
        }

        private void ShowNewOptimumWindow(string lastOptimum, string optimum, string changeType)
        {
            MessageBox.Show($"New optimum found: {optimum} instead of {lastOptimum} ({changeType})", "Rate listener", MessageBoxButton.OK,
                MessageBoxImage.Exclamation, MessageBoxResult.OK, options: MessageBoxOptions.DefaultDesktopOnly);
        }

        private void FindChain(string from, string to, Rate baseRate, List<Rate> availableRates, List<ChainLink> chain)
        {
            if (from.IsNullOrEmpty() || to.IsNullOrEmpty())
            {
                return;
            }

            var link = new ChainLink();
            if (baseRate.BuyCode == from)
            {
                link.From = baseRate.BuyCode;
                link.To = baseRate.SellCode;
                link.Fx = baseRate.BuyRate;
            }
            else if (baseRate.SellCode == from)
            {
                link.From = baseRate.SellCode;
                link.To = baseRate.BuyCode;
                link.Fx = 1.0 / baseRate.SellRate;
            }
            else
            {
                return;
            }
            availableRates = availableRates.ToList();

            link.Rate = baseRate;
            chain.Add(link);
            if (link.To == to)
            {
                return;
            }
            availableRates.RemoveAll(r => r == baseRate || r.IsCurrUsed(from));
            var existingChain = chain.ToList();
            var nextRates = availableRates.Where(r => !r.IsCurrUsed(from) && !r.IsCurrUsed(link.From) && r.IsCurrUsed(link.To)).ToArray();
            for (var i = 0; i < nextRates.Length; i++)
            {
                if (i == 0)
                {
                    FindChain(link.To, to, nextRates[0], availableRates, chain);
                }
                else
                {
                    var newChain = existingChain.ToList();
                    FindChain(link.To, to, nextRates[i], availableRates, newChain);
                    if (newChain.Last().To == to)
                    {
                        chainList.Add(new Chain(newChain));
                    }
                }
                availableRates.Remove(nextRates[i]);
            }
        }

        public ICommand ExchangeCurrenciesCommand { get; }
        public ObservableCollection<Rate> Rates { get; } = new();
        public ObservableCollection<Chain> Chains { get; } = new();
        public ObservableCollection<string> Currencies { get; } = new();

        private readonly DispatcherTimer timer = new();

        public bool IsReceiving
        {
            get => GetVal<bool>();
            set => SetVal(value);
        }

        private void StartListening()
        {
            Task.Run(UpdateData);
        }

        public string SellingAmount
        {
            get => GetVal<string>("0");
            set
            {
                SetVal(value, StoreSettings);
                RaiseAll();
            }
        }

        public string BuyingAmount
        {
            get => GetVal<string>("0");
            set
            {
                SetVal(value, StoreSettings);
                RaiseAll();
            }
        }

        public string FromCurrCalculated
        {
            get
            {
                if (chainList.Count > 0 && ParseDouble(BuyingAmount, out var to))
                {
                    var from = to / LastEffectiveRate;
                    if (IsToFeeIncluded && ParseDouble(ToFee, out var fee))
                    {
                        FromFeeCalculated = from * fee / 100.0;
                        from += FromFeeCalculated;
                    }
                    return $"{from:N2}".Replace(',', ' ');
                }
                return string.Empty;
            }
        }

        public string BuyingToDisplay =>
            FromCurrCalculated.IsFilled() && BuyingAmount != "0"
                ? $"{BuyingAmount} => {FromCurrCalculated}"
                : string.Empty;
        
        public double FromFeeCalculated { get; private set; }
        
        public double ToFeeCalculated { get; private set; }

        private static bool ParseDouble(string inS, out double result) => 
            double.TryParse(inS.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out result);

        public string ToCurrCalculated
        {
            get
            {
                if (chainList.Count > 0 && ParseDouble(SellingAmount, out var from))
                {
                    var to = from * LastEffectiveRate;
                    if (IsFromFeeIncluded && ParseDouble(FromFee, out var fee))
                    {
                        ToFeeCalculated = to * fee / 100.0;
                        to -= ToFeeCalculated;
                    }

                    return $"{to:N2}".Replace(',', ' ');
                }
                return string.Empty;
            }
        }
        
        public string SellingToDisplay =>
            ToCurrCalculated.IsFilled() && SellingAmount != "0"
                ? $"{SellingAmount} => {ToCurrCalculated}"
                : string.Empty;

        private double LastEffectiveRate
        {
            get => GetVal<double>(-1.0);
            set
            {
                if (SetVal(value) && value > 0)
                {
                    RaisePropertyChanged(nameof(RateToDisplay));
                    RaisePropertyChanged(nameof(LastOptimum));
                    RaisePropertyChanged(nameof(LastOptimumPrecised));
                    StoreSettings();
                    var logMessage = $"{SearchFromCurr} => {SearchToCurr}: {LastOptimum}";
                    InverseRate = FindChains(true);
                    if (InverseRate > 0)
                    {
                        logMessage += $"; {SearchToCurr} => {SearchFromCurr}: {InverseRate.RateToDisplay()}";
                    }
                    Logger.Log(logMessage);
                }
            }
        }

        public string RateToDisplay => 
            LastEffectiveRate.RateToDisplay();

        private double InverseRate
        {
            get => GetVal<double>();
            set => SetVal(value, StoreSettings);
        }

        public string LastOptimum => LastEffectiveRate.RateToDisplay();
        public string LastOptimumPrecised => LastEffectiveRate.RateToDisplay(true);

        public string SearchFromCurr
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, StoreSettings);
                FindChains();
                RaiseAll();
            }
        }

        public string SearchToCurr
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, StoreSettings);
                FindChains();
                RaiseAll();
            }
        }

        public string Direction =>
            SearchFromCurr.IsFilled() && SearchToCurr.IsFilled()
            ? $"{SearchFromCurr} => {SearchToCurr}"
            : string.Empty;
        
        public DateTime LastUpdateTime
        {
            get => GetVal<DateTime>();
            private set => SetVal(value);
        }

        public bool IsImprovementAlert
        {
            get => GetVal<bool>();
            set => SetVal(value, StoreSettings);
        }

        public bool IsDepreciationAlert
        {
            get => GetVal<bool>();
            set => SetVal(value, StoreSettings);
        }

        public bool IsFromFeeIncluded
        {
            get => GetVal<bool>();
            set
            {
                SetVal(value, StoreSettings);
                RaiseAll();
            }
        }

        public string FromFee
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, StoreSettings);
                RaiseAll();
            }
        }

        public bool IsToFeeIncluded
        {
            get => GetVal<bool>();
            set
            {
                SetVal(value, StoreSettings);
                RaiseAll();
            }
        }

        public string ToFee
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, StoreSettings);
                RaiseAll();
            }
        }

        public bool IsAlertWhenMoreChecked
        {
            get => GetVal<bool>();
            set => SetVal(value, StoreSettings);
        }

        public string AlertWhenMore
        {
            get => GetVal<string>();
            set => SetVal(value, StoreSettings);
        }

        public BankProvider? SelectedBankProvider
        {
            get => GetVal<BankProvider>();
            set => SetVal(value, StartListening);
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        
        public List<BankProvider> SupportedBankProviders => 
            BankProvider.SupportedBankProviders;
        
        private void RaiseAll()
        {
            RaisePropertyChanged(nameof(FromCurrCalculated));
            RaisePropertyChanged(nameof(BuyingToDisplay));
            RaisePropertyChanged(nameof(FromFeeCalculated));

            RaisePropertyChanged(nameof(ToCurrCalculated));
            RaisePropertyChanged(nameof(SellingToDisplay));
            RaisePropertyChanged(nameof(ToFeeCalculated));
        }
    }
}
