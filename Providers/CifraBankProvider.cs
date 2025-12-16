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

        var ratesResponse = new RatesResponse()
        {
            Received = DateTime.UtcNow
        };
        var rates = new List<Rate>
        {
            new()
            {
                BuyCode = "USD",
                BuyRate = ToDouble(response.NoncashUsdSell),
                SellCode = "RUB",
                SellRate = ToDouble(response.NoncashUsdBuy),
            },
            new()
            {
                BuyCode = "EUR",
                BuyRate = ToDouble(response.NoncashEurSell),
                SellCode = "RUB",
                SellRate = ToDouble(response.NoncashEurBuy),
            },
            new()
            {
                BuyCode = "RUB",
                BuyRate = 1 / ToDouble(response.NoncashKztBuy),
                SellCode = "KZT",
                SellRate = 1 / ToDouble(response.NoncashKztSell),
            },
            new()
            {
                BuyCode = "CNY",
                BuyRate = ToDouble(response.NoncashCnySell),
                SellCode = "RUB",
                SellRate = ToDouble(response.NoncashCnyBuy),
            },
        };

        ratesResponse.Data = new RateContainer
        {
            Mobile = rates.ToArray(),
            Cash = [],
            NonCash = [],
        };
        return ratesResponse;
        
        double ToDouble(string str) => 
            double.Parse(str.Replace(",", "."), NumberStyles.Currency);
    }
}