using Microsoft.Extensions.Caching.Memory;

namespace UserAuthentication_ASPNET.Services.Utils;

public static class RateLimitUtil
{
    private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public static bool IsRateLimitExceeded(string userIp, string endpoint)
    {
        var key = $"{userIp}:{endpoint}";

        if (_cache.TryGetValue(key, out int currentCount))
        {
            if (currentCount >= 3)
            {
                return true;
            }

            _cache.Set(key, currentCount + 1, TimeSpan.FromMinutes(1));
        }
        else
        {
            _cache.Set(key, 1, TimeSpan.FromMinutes(1));
        }

        return false;
    }
}
