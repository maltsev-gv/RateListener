using System.Collections.Concurrent;
using System.Collections.Generic;
using RateListener.Models;
using RateListener.Providers;

namespace RateListener.Helpers;

public class CacheHelper
{
    private static readonly ConcurrentDictionary<IRatesProvider, RatesResponse> ResponseCache = new();
    public static RatesResponse GetCachedResponse(IRatesProvider provider) =>
        ResponseCache.GetValueOrDefault(provider);

    public static void StoreResponse(IRatesProvider provider, RatesResponse response) =>
        ResponseCache[provider] = response;
}