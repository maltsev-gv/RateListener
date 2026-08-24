using System;
using System.Collections.Generic;
using System.Globalization;
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
                RotateOldSegments();

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

    private static DateTime lastRotationCheck = DateTime.MinValue;

    private static void RotateOldSegments()
    {
        if ((DateTime.Now - lastRotationCheck).TotalHours < 1)
            return;
        lastRotationCheck = DateTime.Now;

        var currentSegmentStart = RatesArchive.SegmentStart(DateTime.Now);
        var changed = false;
        foreach (var segmentStart in EnumerateStaleSegmentStarts(currentSegmentStart))
        {
            RatesArchive.MergeIntoSegment(
                Path.Combine(RatesArchive.ArchiveDirectory, $"{segmentStart:yyyy-MM-dd}.zip"),
                segmentStart, ratesToStore);

            foreach (var container in ratesToStore.Values)
            {
                foreach (var direction in container.Directions)
                {
                    direction.Rates.RemoveAll(r =>
                        !RatesArchive.TryParseTime(r.Time, out var t) ||
                        t.Year < RatesArchive.SegmentEpoch.Year);
                    direction.Rates.RemoveAll(r =>
                        RatesArchive.TryParseTime(r.Time, out var t) &&
                        RatesArchive.SegmentStart(t) == segmentStart);
                }
                container.Directions.RemoveAll(d => d.Rates.Count == 0);
            }
            changed = true;
        }

        if (!changed)
            return;
        foreach (var container in ratesToStore.Values.ToList())
            container.Directions.RemoveAll(d => d.Rates.Count == 0);
        ratesToStore = ratesToStore
            .Where(kvp => kvp.Value.Directions.Count > 0)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        isRatesChanged = true;
    }

    private static List<DateTime> EnumerateStaleSegmentStarts(DateTime currentSegmentStart)
    {
        var starts = new SortedSet<DateTime>();
        foreach (var container in ratesToStore.Values)
        foreach (var direction in container.Directions)
        foreach (var storedRate in direction.Rates)
            if (RatesArchive.TryParseTime(storedRate.Time, out var t) &&
                t.Year >= RatesArchive.SegmentEpoch.Year)
            {
                var segmentStart = RatesArchive.SegmentStart(t);
                if (segmentStart < currentSegmentStart)
                    starts.Add(segmentStart);
            }
        return [.. starts];
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

            RotateOldSegments();
            if (isRatesChanged)
                File.WriteAllText(RatesFile.FullName, JsonHelper.GetSerializedString(ratesToStore.Values));

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

            const double minStoreStepMinutes = 5;
            const double minChangePercent = 1;
            var rates = storedRates.Rates;
            StoredRate lastStored = null;
            if (rates.Count > 0 && RatesArchive.TryParseTime(rates[^1].Time, out var lastTime) &&
                (viewModel.LastUpdateTime - lastTime).TotalMinutes < minStoreStepMinutes)
                lastStored = rates[^1];

            var newRateValue = viewModel.LastEffectiveRate.RateToDisplay(true);
            bool shouldStore;
            if (viewModel.LastEffectiveRate <= 0 || viewModel.LastUpdateTime == default)
                shouldStore = false;
            else if (lastStored == null)
                shouldStore = true;
            else if (lastStored.Rate == newRateValue)
                shouldStore = false;
            else if (TryParseRateValue(lastStored.Rate, out var oldValue) &&
                     TryParseRateValue(newRateValue, out var newValue))
                shouldStore = Math.Abs(newValue - oldValue) >= oldValue * minChangePercent / 100.0;
            else
                shouldStore = true;

            if (shouldStore)
            {
                rates.Add(new StoredRate
                {
                    Rate = newRateValue,
                    InversedRate = viewModel.FindChains(true)
                        .ToString("0.#####", CultureInfo.InvariantCulture),
                    Time = $"{viewModel.LastUpdateTime:dd.MM.yyyy HH:mm:ss}"
                });
                isRatesChanged = true;
            }
        }

        Timer.Stop();
        Timer.Start();
    }

    private static bool TryParseRateValue(string source, out double value)
    {
        var cleaned = source?.Replace(" ", string.Empty).Replace("\u00A0", string.Empty);
        if (cleaned == null)
        {
            value = 0;
            return false;
        }
        if (cleaned.Contains('.') && cleaned.Contains(','))
            cleaned = cleaned.Replace(",", string.Empty);
        else
            cleaned = cleaned.Replace(',', '.');
        return double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value) && value > 0;
    }

    public static List<StoredRate> GetStoredRates(Guid listenerId, string direction, DateTime? from = null)
    {
        lock (ConfigFile)
        {
            var result = ratesToStore.GetValueOrDefault(listenerId)?.Directions
                .FirstOrDefault(d => d.Direction == direction)?.Rates.ToList() ?? [];

            if (from != null)
            {
                result.AddRange(RatesArchive.EnumerateArchivesCovering(from.Value)
                    .SelectMany(RatesArchive.ReadSegment)
                    .Where(c => c.ListenerId == listenerId)
                    .SelectMany(c => c.Directions)
                    .Where(d => d.Direction == direction)
                    .SelectMany(d => d.Rates));
            }

            return result;
        }
    }

    public static ListenerSettingsViewModel ToViewModel(this SettingsInfo settings)
    {
        var viewModel = settings.Adapt<ListenerSettingsViewModel>();
        if (viewModel.PollIntervalSec <= 0 && settings.BankProviderName.IsFilled())
            viewModel.PollIntervalSec = BankProvider.DefaultPollIntervalSec(settings.BankProviderName);
        return viewModel;
    }
    
    private static void ConfigureMapster()
    {
        TypeAdapterConfig<ListenerSettingsViewModel, StoredRatesContainer>.NewConfig()
            .Map(rc => rc.ListenerId, vm => vm.Id);
    }
}

 