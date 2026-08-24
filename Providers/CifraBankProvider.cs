using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Flurl.Http;
using RateListener.Helpers;
using RateListener.Models;

namespace RateListener.Providers;

internal class CifraBankProvider : IRatesProvider
{
    public string Name => "Цифра банк";
    public string Url => Properties.Resources.ResourceManager.GetString("CifraBankProviderUrlJson");

    public async Task<RatesResponse> GetRatesResponse()
    {
        var json = await Url.GetStringAsync();
        var response = JsonHelper.GetObjectFromString<CifraBankRates>(json);

        if (response == null)
        {
            ParseDiagnostics.SaveFailedSource(Name, json);
            throw new InvalidOperationException($"{Name}: response is not parseable");
        }

        var ratesResponse = new RatesResponse()
        {
            Received = DateTime.UtcNow
        };
        var rates = new List<Rate>();
        AddPair(rates, "USD", ToDouble(response.NoncashUsdSell), "RUB", ToDouble(response.NoncashUsdBuy));
        AddPair(rates, "EUR", ToDouble(response.NoncashEurSell), "RUB", ToDouble(response.NoncashEurBuy));
        AddPair(rates, "CNY", ToDouble(response.NoncashCnySell), "RUB", ToDouble(response.NoncashCnyBuy));

        var kztBuy = ToDouble(response.NoncashKztBuy);
        var kztSell = ToDouble(response.NoncashKztSell);
        if (kztBuy > 0 && kztSell > 0)
            rates.Add(new Rate
            {
                BuyCode = "RUB",
                BuyRate = 1 / kztBuy,
                SellCode = "KZT",
                SellRate = 1 / kztSell
            });

        if (rates.Count == 0)
        {
            ParseDiagnostics.SaveFailedSource(Name, json);
            throw new InvalidOperationException($"{Name}: response contains no usable rates");
        }

        ratesResponse.Data = new RateContainer
        {
            Mobile = rates.ToArray(),
        };
        return ratesResponse;

        double ToDouble(string str) =>
            double.TryParse(str?.Replace(",", "."), NumberStyles.Currency, CultureInfo.InvariantCulture, out var v)
                ? v
                : 0;
    }

    private static void AddPair(List<Rate> rates, string buyCode, double buyRate, string sellCode, double sellRate)
    {
        if (buyRate > 0 && sellRate > 0)
            rates.Add(new Rate { BuyCode = buyCode, BuyRate = buyRate, SellCode = sellCode, SellRate = sellRate });
    }
}