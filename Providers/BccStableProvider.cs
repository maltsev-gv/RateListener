using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using RateListener.Helpers;
using RateListener.Models;

namespace RateListener.Providers;

internal class BccStableProvider : IRatesProvider
{
    public string Url => Properties.Resources.ResourceManager.GetString("BccStableProviderUrlHtml");
    public string Name => "Center Credit (stable)";

    public async Task<RatesResponse> GetRatesResponse()
    {
        var html = await Url.GetStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var ratesResponse = new RatesResponse()
        {
            Received = DateTime.UtcNow
        };
        var rateNodes = FindRateNodes(doc);
        var rates = new List<Rate>();
        foreach (var rateNode in rateNodes)
        {
            var rate = new Rate { SellCode = Currencies.Kzt };

            // BuyCode
            var buyCode = string.Empty;
            var titleNode = rateNode.SelectSingleNode(".//div[contains(@class, 'item-title')]");
            if (titleNode != null)
            {
                var pairText = titleNode.InnerText.Trim();
                if (pairText.Contains("/ KZT"))
                {
                    var parts = pairText.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length > 0)
                        buyCode = parts[0].ToUpperInvariant();
                }
            }
            rate.BuyCode = buyCode;

            // BuyRate Продать
            var buyRateValue = 0d;
            var buyRateNode = rateNode.SelectSingleNode(".//div[contains(@class, 'item-num')][.//div[contains(@class, 'item-head-title') and contains(text(), 'Продать')]]//div[contains(@class, 'items-mb')][1]");
            if (buyRateNode != null)
            {
                var valStr = buyRateNode.InnerText.Trim().Replace(" ", "");
                if (double.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    buyRateValue = v;
            }
            rate.BuyRate = buyRateValue;

            // SellRate Купить
            var sellRateValue = 0d;
            var sellRateNode = rateNode.SelectSingleNode(".//div[contains(@class, 'item-num')][.//div[contains(@class, 'item-head-title') and contains(text(), 'Купить')]]//div[contains(@class, 'items-mb')][1]");
            if (sellRateNode != null)
            {
                var valStr = sellRateNode.InnerText.Trim().Replace(" ", "");
                if (double.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    sellRateValue = v;
            }
            rate.SellRate = sellRateValue;

            if (!string.IsNullOrEmpty(buyCode) && buyRateValue > 0 && sellRateValue > 0)
                rates.Add(rate);
        }

        if (rates.Count == 0)
        {
            ParseDiagnostics.SaveFailedSource(Name, html);
            throw new InvalidOperationException($"{Name}: layout changed, 0 pairs parsed");
        }

        ratesResponse.Data = new RateContainer
        {
            Mobile = rates.ToArray(),
        };
        return ratesResponse;
    }

    private static HtmlNode[] FindRateNodes(HtmlDocument doc)
    {
        const string nalText = "Наличные валюты в отделении";
        const string northText = "Наличные курсы в северном регионе";

        var xpath = "//div[contains(@class, 'item-wrap') and contains(., '/ KZT') " +
                    "and count(preceding::h3[contains(text(), '" + nalText + "')]) > 0 " +
                    "and count(preceding::h3[contains(text(), '" + northText + "')]) = 0]";
        return doc.DocumentNode.SelectNodes(xpath)?.ToArray() ?? [];
    }
}