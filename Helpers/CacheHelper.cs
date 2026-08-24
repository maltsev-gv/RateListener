using System.Collections.Concurrent;
using System.Collections.Generic;
using RateListener.Models;
using RateListener.Providers;

namespace RateListener.Helpers;

public class CacheHelper
{
    private static readonly ConcurrentDictionary<IRatesProvider, RatesResponse> GoodResponseCache = new();
    private static readonly ConcurrentDictionary<IRatesProvider, RatesResponse> ErrorResponseCache = new();

    public static RatesResponse GetCachedResponse(IRatesProvider provider) =>
        GoodResponseCache.GetValueOrDefault(provider);

    public static RatesResponse GetLastErrorResponse(IRatesProvider provider) =>
        ErrorResponseCache.GetValueOrDefault(provider);

    public static void StoreResponse(IRatesProvider provider, RatesResponse response)
    {
        if (response.Success)
        {
            GoodResponseCache[provider] = response;
            ErrorResponseCache.TryRemove(provider, out _);
        }
        else
            ErrorResponseCache[provider] = response;
    }
}
