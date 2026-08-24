using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using RateListener.ExtensionMethods;
using RateListener.Helpers;
using RateListener.Models;

namespace RateListener.Providers
{
    internal class BccFxProvider : IRatesProvider
    {
        protected virtual bool InvertedBuyAndSellRates => false;
        public virtual string Name => "Center Credit (FX)";
        public virtual string Url => Properties.Resources.ResourceManager.GetString("BccFxProviderUrlHtml");

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
            var regex = new Regex(@"([\d\.\,]* ?[\d\.\,])*");
            var rates = new List<Rate>();
            foreach (var rateNode in rateNodes)
            {
                var currNameNode = rateNode.SelectSingleNode(@"./div");
                if (currNameNode == null)
                    continue;
                var buyCode = currNameNode.InnerText.Trim(' ', '\n', '\t');
                if (buyCode.IsNullOrEmpty())
                    continue;

                var text = rateNode.InnerText.Replace('\t', ' ').Replace('\n', ' ').Replace(buyCode, " ").Trim();
                var matches = regex.Matches(text).Where(m => m.Value != "").ToArray();
                if (matches.Length < 2)
                    continue;
                if (!double.TryParse(matches[InvertedBuyAndSellRates ? 1 : 0].Value.Replace(" ", ""),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var buyRate) || buyRate <= 0)
                    continue;
                if (!double.TryParse(matches[InvertedBuyAndSellRates ? 0 : 1].Value.Replace(" ", ""),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out var sellRate) || sellRate <= 0)
                    continue;

                rates.Add(new Rate
                {
                    BuyCode = buyCode,
                    SellCode = Currencies.Kzt,
                    BuyRate = buyRate,
                    SellRate = sellRate
                });
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

    protected virtual HtmlNode[] FindRateNodes(HtmlDocument doc)
    {
        var mobileFxNode = doc.DocumentNode
            .SelectSingleNode(@".//div[contains(@class,'text-lg') and .//div[contains(text(),'Валюта')]  and .//div[contains(text(),'Купить')]]")
            ?.SelectSingleNode(@".//div[contains(@class,'text-dark')]");
        return mobileFxNode?.SelectNodes(@".//div[contains(@class,'mb-9')]")?.ToArray() ?? [];
    }
    }
}
