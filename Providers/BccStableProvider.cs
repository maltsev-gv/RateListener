using System.Linq;
using HtmlAgilityPack;

namespace RateListener.Providers
{
    internal class BccStableProvider : BccFxProvider
    {
        protected override bool InvertedBuyAndSellRates => false;

        public override string Url => Properties.Resources.ResourceManager.GetString("BccStableProviderUrlHtml");
        public override string Name => "Center Credit (stable)";

        protected override HtmlNode[] FindRateNodes(HtmlDocument doc)
        {
            var mobileFxNode = doc.DocumentNode
                .SelectSingleNode(@".//div[contains(@class,'exchange-card') and .//div[contains(text(),'bcc.kz')]]");
            var rateNodes = mobileFxNode.SelectNodes(@".//div[contains(@class,'exchange-card-list') and .//div[contains(@class,'items-center')]]").ToArray();
            return rateNodes;
        }
    }
}
