using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Flurl.Http;
using RateListener.ExtensionMethods;
using RateListener.Helpers;

namespace RateListener.Providers;

public static class NbkRates
{
    private const string RatesUrl = "https://nationalbank.kz/rss/rates_all.xml";

    private static readonly object Locker = new();
    private static Dictionary<string, double> ratesPerUnit = [];
    private static DateTime loadedDate = DateTime.MinValue;

    public static Dictionary<string, double> CachedRates
    {
        get
        {
            lock (Locker)
            {
                return new Dictionary<string, double>(ratesPerUnit);
            }
        }
    }

    public static async Task<Dictionary<string, double>> GetRatesAsync()
    {
        lock (Locker)
        {
            if (loadedDate == DateTime.Today && ratesPerUnit.Count > 0)
                return CachedRates;
        }

        try
        {
            var xml = await RatesUrl.GetStringAsync();
            var parsed = XDocument.Parse(xml).Descendants("item")
                .Select(item => new
                {
                    Code = item.Element("title")?.Value.Trim().ToUpperInvariant(),
                    Quant = ParseDouble(item.Element("quant")?.Value),
                    Rate = ParseDouble(item.Element("description")?.Value)
                })
                .Where(x => x.Code != null && x.Rate > 0 && x.Quant > 0)
                .ToDictionary(x => x.Code!, x => x.Rate / x.Quant);

            if (parsed.Count == 0)
                return CachedRates;

            lock (Locker)
            {
                ratesPerUnit = parsed;
                loadedDate = DateTime.Today;
            }
            return CachedRates;
        }
        catch (Exception ex)
        {
            Logger.Log($"NBK reference rates could not be loaded: {ex.Message}");
            return CachedRates;
        }
    }

    public static bool TryGetCrossRate(string from, string to, out double crossRate)
    {
        crossRate = 0;
        var rates = CachedRates;
        if (from.IsNullOrEmpty() || to.IsNullOrEmpty() ||
            !rates.TryGetValue(from, out var fromToKzt) || !rates.TryGetValue(to, out var toToKzt))
            return false;
        if (toToKzt <= 0)
            return false;
        crossRate = fromToKzt / toToKzt;
        return true;
    }

    private static double ParseDouble(string source) =>
        double.TryParse(source?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
}
