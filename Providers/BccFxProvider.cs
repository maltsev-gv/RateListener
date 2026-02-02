using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
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
                var rate = new Rate {SellCode = Currencies.Kzt};
                var currNameNode = rateNode.SelectSingleNode(@"./div");
                rate.BuyCode = currNameNode.InnerText.Trim(' ', '\n', '\t');
                var text = rateNode.InnerText.Replace('\t', ' ').Replace('\n', ' ').Replace(rate.BuyCode, " ").Trim();
                var matches = regex.Matches(text).Where(m => m.Value != "").ToArray();
                rate.BuyRate = double.Parse(matches[InvertedBuyAndSellRates ? 1 : 0].Value.Replace(" ", ""));
                rate.SellRate = double.Parse(matches[InvertedBuyAndSellRates ? 0 : 1].Value.Replace(" ", ""));
                rates.Add(rate);
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
                .SelectSingleNode(@".//div[contains(@class,'text-dark')]");
            var rateNodes = mobileFxNode.SelectNodes(@".//div[contains(@class,'mb-9')]").ToArray();
            return rateNodes;
        }
    }
}
