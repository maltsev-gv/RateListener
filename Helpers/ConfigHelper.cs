using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Mapster;
using Newtonsoft.Json;
using RateListener.Models;
using RateListener.ViewModels;

namespace RateListener.Helpers;

public static class ConfigHelper
{
    private static readonly FileInfo ConfigFile;
    private static readonly Timer Timer = new(500) { AutoReset = false };
    private static Dictionary<Guid, SettingsInfo> settingsToStore = [];

    static ConfigHelper()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly()
            .Location);
        ConfigFile = new FileInfo(Path.Combine(path!, "RateListener.config.json"));
        Timer.Elapsed += TimerOnElapsed;
        
        TypeAdapterConfig<SettingsInfo, ListenerSettingsViewModel>.NewConfig()
            .Ignore(vm => vm.SelectedBankProvider)
            .AfterMapping((si, vm) =>
                vm.SelectedBankProvider =
                    BankProvider.SupportedBankProviders.FirstOrDefault(bp => bp.Name == si.BankProviderName));
        TypeAdapterConfig<ListenerSettingsViewModel, SettingsInfo>.NewConfig()
            .Map(
                si => si.BankProviderName,
                vm => vm.SelectedBankProvider.Name);
    }

    public static bool IsLoading { get; set; }

    private static void TimerOnElapsed(object sender, ElapsedEventArgs e)
    {
        if (settingsToStore is not null)
        {
            lock (ConfigFile)
            {
                File.WriteAllText(ConfigFile.FullName, JsonHelper.GetSerializedString(settingsToStore.Values));
            }
        }
    }

    public static List<SettingsInfo> LoadSettings()
    {
        if (!ConfigFile.Exists)
        {
            return null;
        }

        var content = File.ReadAllText(ConfigFile.FullName);
        settingsToStore = JsonConvert.DeserializeObject<List<SettingsInfo>>(content)
            .ToDictionary(si => si.Id, si => si);
        return settingsToStore.Values.ToList();
    }

    public static void SaveSettings(ListenerSettingsViewModel viewModel)
    {
        if (IsLoading)
        {
            return;
        }

        if (!settingsToStore.TryGetValue(viewModel.Id, out var settings))
        {
            settingsToStore[viewModel.Id] = viewModel.Adapt<SettingsInfo>();
        }
        else
        {
            viewModel.Adapt(settings);
        }

        Timer.Stop();
        Timer.Start();
    }

    public static ListenerSettingsViewModel ToViewModel(this SettingsInfo settings) =>
        settings.Adapt<ListenerSettingsViewModel>();
}

 