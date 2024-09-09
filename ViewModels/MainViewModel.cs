using RateListener.ExtensionMethods;
using RateListener.Helpers;
using RateListener.Models;
using RateListener.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RateListener.Providers;

namespace RateListener.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Configuration _config;
        public MainViewModel()
        {
            _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None); // Add an Application Setting.

            StartParseCommand = new RelayCommand(StartListening);
            ExchangeCurrenciesCommand = new RelayCommand(ExchangeCurrencies);
            _timer.Interval = TimeSpan.FromMinutes(1);
            _timer.Tick += _timer_Tick;
            _timer.Start();
            SearchFromCurr = ReadSetting(nameof(SearchFromCurr));
            SearchToCurr = ReadSetting(nameof(SearchToCurr));
            CalcFrom = ReadSetting(nameof(CalcFrom));
            CalcTo = ReadSetting(nameof(CalcTo));
            FromFee = ReadSetting(nameof(FromFee));
            ToFee = ReadSetting(nameof(ToFee));
            AlertWhenMore = ReadSetting(nameof(AlertWhenMore)) ?? "0";

            LastEffectiveRate = -1;
            LastEffectiveRate = ReadTypedSetting<double?>(nameof(LastEffectiveRate)) ?? -1;

            IsImprovementAlert = ReadTypedSetting<bool?>(nameof(IsImprovementAlert)) ?? true;
            IsDepreciationAlert = ReadTypedSetting<bool?>(nameof(IsDepreciationAlert)) ?? false;
            IsFromFeeIncluded = ReadTypedSetting<bool?>(nameof(IsFromFeeIncluded)) ?? false;
            IsToFeeIncluded = ReadTypedSetting<bool?>(nameof(IsToFeeIncluded)) ?? false;
            IsAlertWhenMoreChecked = ReadTypedSetting<bool?>(nameof(IsAlertWhenMoreChecked)) ?? false;

            StartParseCommand.Execute(this);
        }

        private void ExchangeCurrencies(object obj)
        {
            (SearchToCurr, SearchFromCurr) = (SearchFromCurr, SearchToCurr);
        }

        private string ReadSetting(string settingName) => _config.AppSettings.Settings[settingName]?.Value;

        private T? ReadTypedSetting<T>(string settingName)
        {
            Func<string, object>? f =
                typeof(T) == typeof(bool?)
                    ? (s) => bool.TryParse(s, out bool res) ? res : null
                : typeof(T) == typeof(double?)
                    ? (s) => double.TryParse(s, out double res) ? res : null
                : null;
            
            var val = f?.Invoke(ReadSetting(settingName));

            return (T)val;

        }

        private void StoreSetting(object val, [CallerMemberName] string settingName = null)
        {
            if (_config.AppSettings.Settings[settingName] == null)
            {
                _config.AppSettings.Settings.Add(settingName, val.ToString());
            }
            else
            {
                _config.AppSettings.Settings[settingName].Value = val.ToString();
            }
            _config.Save();
        }

        private async void _timer_Tick(object? sender, EventArgs e)
        {
            await ReceiveRatesAsync();
            FindChains();
            RaisePropertyChanged(nameof(FromCurrCalculated));
            RaisePropertyChanged(nameof(ToCurrCalculated));
        }

        public string Url { get; set; }
        protected async Task ReceiveRatesAsync()
        {
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

                FindChains();
                RaisePropertyChanged(nameof(FromCurrCalculated));
                RaisePropertyChanged(nameof(ToCurrCalculated));
            });
            IsReceiving = false;
        }

        private List<Chain> _chainList = new();
        private string _prevCurrs = string.Empty;

        private double FindChains(bool getInversedRate = false)
        {
            if (Rates.Count == 0 || SearchFromCurr.IsNullOrEmpty() || SearchToCurr.IsNullOrEmpty() || SearchFromCurr == SearchToCurr)
            {
                return 0;
            }

            bool isCurrChanged = _prevCurrs.IsFilled() && _prevCurrs != $"{SearchFromCurr}_{SearchToCurr}";
            _prevCurrs = $"{SearchFromCurr}_{SearchToCurr}";

            var oldList = _chainList.ToList();
            _chainList.Clear();
            foreach (var rate in Rates.Where(r => r.IsCurrUsed(getInversedRate ? SearchToCurr : SearchFromCurr)))
            {
                var chain = new List<ChainLink>();
                FindChain(getInversedRate ? SearchToCurr : SearchFromCurr,
                          getInversedRate ? SearchFromCurr : SearchToCurr, 
                          rate, Rates.Where(r => r != rate).ToList(), chain);
                if (chain.LastOrDefault()?.To == (getInversedRate ? SearchFromCurr : SearchToCurr))
                {
                    _chainList.Add(new Chain(chain));
                }
            }

            _chainList = _chainList.OrderByDescending(c => c.EffectiveRate).ToList();
            var bestRate = _chainList.FirstOrDefault()?.EffectiveRate ?? 0;

            if (getInversedRate)
            {
                _chainList = oldList.ToList();
                return bestRate;
            }

            RunInMainThread(() =>
            {
                Chains.Clear();
                _chainList.ForEach(c => Chains.Add(c));

                if (Currencies.Count == 0)
                {
                    Rates.SelectMany(r => new[] { r.BuyCode, r.SellCode }).Distinct().OrderBy(s => s).ToList().ForEach(c => Currencies.Add(c));
                }
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
                        _chainList.Add(new Chain(newChain));
                    }
                }
                availableRates.Remove(nextRates[i]);
            }
        }

        public ICommand StartParseCommand { get; }
        public ICommand ExchangeCurrenciesCommand { get; }
        public ObservableCollection<Rate> Rates { get; } = new();
        public ObservableCollection<Chain> Chains { get; } = new();
        public ObservableCollection<string> Currencies { get; } = new();
        
        DispatcherTimer _timer = new();

        public bool IsReceiving
        {
            get => GetVal<bool>();
            set => SetVal(value);
        }

        private void StartListening(object? commandParameter)
        {
            _ = Task.Run(ReceiveRatesAsync);
        }

        public string CalcFrom
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, () => StoreSetting(value));
                RaisePropertyChanged(nameof(ToCurrCalculated));
            }
        }

        public string CalcTo
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, () => StoreSetting(value));
                RaisePropertyChanged(nameof(FromCurrCalculated));
            }
        }

        public string FromCurrCalculated
        {
            get
            {
                if (_chainList.Count > 0 && ParseDouble(CalcTo, out var to))
                {
                    var from = to / LastEffectiveRate;
                    if (IsToFeeIncluded && ParseDouble(ToFee, out var fee))
                    {
                        FromFeeCalculated = from * fee / 100.0;
                        from += FromFeeCalculated;
                        RaisePropertyChanged(nameof(FromFeeCalculated));
                    }
                    return $"{from:N2}";
                }
                return string.Empty;
            }
        }

        public double FromFeeCalculated { get; private set; }
        
        public double ToFeeCalculated { get; private set; }

        private static bool ParseDouble(string inS, out double result) => 
            double.TryParse(inS.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out result);

        public string ToCurrCalculated
        {
            get
            {
                if (_chainList.Count > 0 && ParseDouble(CalcFrom, out var from))
                {
                    var to = from * LastEffectiveRate;
                    if (IsFromFeeIncluded && ParseDouble(FromFee, out var fee))
                    {
                        ToFeeCalculated = to * fee / 100.0;
                        to -= ToFeeCalculated;
                        RaisePropertyChanged(nameof(ToFeeCalculated));
                    }

                    return $"{to:N2}";
                }
                return string.Empty;
            }
        }

        public double LastEffectiveRate
        {
            get => GetVal<double>();
            set
            {
                if (SetVal(value) && value > 0)
                {
                    RaisePropertyChanged(nameof(LastOptimum));
                    RaisePropertyChanged(nameof(LastOptimumPrecised));
                    StoreSetting(value);
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

        public double InverseRate
        {
            get => GetVal<double>();
            set => SetVal(value, () => StoreSetting(value));
        }


        public string LastOptimum => LastEffectiveRate.RateToDisplay();
        public string LastOptimumPrecised => LastEffectiveRate.RateToDisplay(true);

        public string SearchFromCurr
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, () => StoreSetting(value));
                FindChains();
                RaisePropertyChanged(nameof(FromCurrCalculated));
                RaisePropertyChanged(nameof(ToCurrCalculated));
            }
        }

        public string SearchToCurr
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, () => StoreSetting(value));
                FindChains();
                RaisePropertyChanged(nameof(FromCurrCalculated));
                RaisePropertyChanged(nameof(ToCurrCalculated));
            }
        }

        public DateTime LastUpdateTime
        {
            get => GetVal<DateTime>();
            set => SetVal(value);
        }

        public bool IsImprovementAlert
        {
            get => GetVal<bool>();
            set => SetVal(value, () => StoreSetting(value));
        }

        public bool IsDepreciationAlert
        {
            get => GetVal<bool>();
            set => SetVal(value, () => StoreSetting(value));
        }

        public bool IsFromFeeIncluded
        {
            get => GetVal<bool>();
            set
            {
                SetVal(value, () => StoreSetting(value));
                RaisePropertyChanged(nameof(ToCurrCalculated));
            }
        }

        public string FromFee
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, () => StoreSetting(value));
                RaisePropertyChanged(nameof(ToCurrCalculated));
            }
        }

        public bool IsToFeeIncluded
        {
            get => GetVal<bool>();
            set
            {
                SetVal(value, () => StoreSetting(value));
                RaisePropertyChanged(nameof(FromCurrCalculated));
            }
        }

        public string ToFee
        {
            get => GetVal<string>();
            set
            {
                SetVal(value, () => StoreSetting(value));
                RaisePropertyChanged(nameof(FromCurrCalculated));
            }
        }

        public bool IsAlertWhenMoreChecked
        {
            get => GetVal<bool>();
            set => SetVal(value, () => StoreSetting(value));
        }

        public string AlertWhenMore
        {
            get => GetVal<string>();
            set => SetVal(value, () => StoreSetting(value));
        }

        public List<BankProvider> SupportedBankProviders { get; } = new List<BankProvider>
        {
            new(new FfinProvider()),
            new(new BccFxProvider()),
            new(new BccStableProvider()),
        };

        public BankProvider SelectedBankProvider
        {
            get => GetVal<BankProvider>(SupportedBankProviders.Last());
            set => SetVal(value, () => StartParseCommand.Execute(this));
        }
    }
}
