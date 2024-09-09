using System.Threading.Tasks;
using Flurl.Http;
using RateListener.Helpers;
using RateListener.Models;

namespace RateListener.Providers
{
    internal class FfinProvider : IRatesProvider
    {
        public string Name => "Freedom Finance";

        public string Url => Properties.Resources.ResourceManager.GetString("FfinProviderUrlJson");

        public async Task<RatesResponse> GetRatesResponse()
        {
            var ratesJson = await Url.GetStringAsync();
            return ZippedJsonHelper.GetObjectFromString<RatesResponse>(ratesJson);
        }
    }
}
