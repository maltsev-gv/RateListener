using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Mapster;
using Newtonsoft.Json;
using RateListener.ExtensionMethods;
using RateListener.Models;
using RateListener.ViewModels;

namespace RateListener.Helpers;

public static class ConfigHelper
{
    private static bool isRatesChanged;
    private static readonly FileInfo ConfigFile;
    private static readonly FileInfo RatesFile;
    private static readonly Timer Timer = new(500) { AutoReset = false };
    private static Dictionary<Guid, SettingsInfo> settingsToStore = [];
    private static Dictionary<Guid, StoredRatesContainer> ratesToStore = [];

    static ConfigHelper()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly()
            .Location);
        ConfigFile = new FileInfo(Path.Combine(path!, "RateListener.config.json"));
        RatesFile = new FileInfo(Path.Combine(path!, "RateListener.rates.json"));
        Timer.Elapsed += TimerOnElapsed;
        
        ConfigureMapster();
    }

    public static bool IsLoading { get; set; }

    private static void TimerOnElapsed(object sender, ElapsedEventArgs e)
    {
        if (OverviewViewModel.IsReceiving)
            return;
        
        try
        {
            lock (ConfigFile)
            {
                File.WriteAllText(ConfigFile.FullName, JsonHelper.GetSerializedString(settingsToStore.Values));
            }

            if (isRatesChanged)
            {
                lock (RatesFile)
                {
                    File.WriteAllText(RatesFile.FullName, JsonHelper.GetSerializedString(ratesToStore.Values));
                }

                isRatesChanged = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Rates or configuration file could not be saved: {ex.Message}");
        }
    }

    public static List<SettingsInfo> LoadSettings()
    {
        if (!ConfigFile.Exists)
        {
            return null;
        }

        lock (ConfigFile)
        {
            if (RatesFile.Exists)
            {
                var ratesContent = File.ReadAllText(RatesFile.FullName);
                ratesToStore = JsonConvert.DeserializeObject<List<StoredRatesContainer>>(ratesContent)
                    .ToDictionary(r => r.ListenerId, r => r);
            }

            var content = File.ReadAllText(ConfigFile.FullName);
            settingsToStore = JsonConvert.DeserializeObject<List<SettingsInfo>>(content)
                .ToDictionary(si => si.Id, si => si);
            return settingsToStore.Values.ToList();
        }
    }

    public static void SaveSettings(ListenerSettingsViewModel viewModel)
    {
        if (IsLoading)
            return;

        lock (ConfigFile)
        {
            if (!settingsToStore.TryGetValue(viewModel.Id, out var settings))
                settingsToStore[viewModel.Id] = viewModel.Adapt<SettingsInfo>();
            else
                viewModel.Adapt(settings);

            if (!ratesToStore.TryGetValue(viewModel.Id, out var ratesContainer))
            {
                ratesContainer = viewModel.Adapt<StoredRatesContainer>();
                ratesToStore[viewModel.Id] = ratesContainer;
            }

            var direction = viewModel.Direction;
            var storedRates = ratesContainer.Directions.FirstOrDefault(d => d.Direction == direction);
            if (storedRates == null)
            {
                storedRates = new DirectionInfo { Direction = direction };
                ratesContainer.Directions.Add(storedRates);
            }

            var lastStoredRate = storedRates.Rates.OrderBy(r => r.Time)
                .LastOrDefault()
                ?.Rate;
            if (lastStoredRate != viewModel.LastEffectiveRate.RateToDisplay(true))
            {
                storedRates.Rates.Add(new StoredRate
                {
                    Rate = viewModel.LastEffectiveRate.RateToDisplay(true),
                    InversedRate = viewModel.FindChains(true)
                        .RateToDisplay(true),
                    Time = $"{viewModel.LastUpdateTime:dd.MM.yyyy HH:mm:ss}"
                });
                isRatesChanged = true;
            }
        }

        Timer.Stop();
        Timer.Start();
    }

    public static ListenerSettingsViewModel ToViewModel(this SettingsInfo settings) =>
        settings.Adapt<ListenerSettingsViewModel>();
    
    private static void ConfigureMapster()
    {
        TypeAdapterConfig<ListenerSettingsViewModel, StoredRatesContainer>.NewConfig()
            .Map(rc => rc.ListenerId, vm => vm.Id);
    }
}

 