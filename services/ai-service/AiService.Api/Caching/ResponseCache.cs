using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace AiService.Api.Caching;

/// <summary>
/// In-process cache for repeated identical AI requests (e.g. two students opening the same
/// lecture chapter both trigger a "generate lecture narration" call with the same source text) —
/// avoids paying for the same Groq completion twice. Deliberately simple: per-instance memory,
/// not shared across replicas. If ai-service is ever scaled to multiple instances, swap this for
/// a Redis-backed IDistributedCache; not needed at this project's scale.
/// </summary>
public sealed class ResponseCache(IMemoryCache cache)
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    public async Task<T> GetOrCreateAsync<T>(string scope, string input, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        var key = BuildKey(scope, input);

        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory();
        cache.Set(key, value, ttl ?? DefaultTtl);
        return value;
    }

    private static string BuildKey(string scope, string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return $"{scope}:{Convert.ToHexString(hash)}";
    }
}
