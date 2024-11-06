using System;
using System.Collections.Concurrent;
using RateListener.Models;
using RateListener.Providers;

namespace RateListener.Helpers;

public class CacheHelper
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);
    private static readonly ConcurrentDictionary<IRatesProvider, RatesResponse> ResponseCache = new();
    public static RatesResponse GetCachedResponse(IRatesProvider provider)
    {
        if (ResponseCache.TryGetValue(provider, out var response) &&
            DateTime.UtcNow - response.Received < CacheDuration)
        {
            return response;
        }

        return null;
    }

    public static void StoreResponse(IRatesProvider provider, RatesResponse response)
    {
        ResponseCache[provider] = response;
    }
}