using System;
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
        var response = JsonHelper.GetObjectFromString<RatesResponse>(ratesJson);

        if (response?.Data == null || (response.Data.Mobile.Length == 0 &&
            response.Data.Cash.Length == 0 && response.Data.NonCash.Length == 0))
        {
            ParseDiagnostics.SaveFailedSource(Name, ratesJson);
            throw new InvalidOperationException($"{Name}: empty rates payload");
        }

        response.Received = DateTime.UtcNow;
        return response;
    }
    }
}
