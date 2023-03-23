using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MultiFactor.SelfService.Linux.Portal.Core.Caching
{
    public class ApplicationCache
    {
        private readonly IMemoryCache _cache;
        private readonly ApplicationCacheConfig _config;

        public ApplicationCache(IMemoryCache cache, IOptions<ApplicationCacheConfig> config)
        {
            _cache = cache;
            _config = config.Value;
        }

        public void Set(string key, string value)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_config.AbsoluteExpiration)
                .SetSize(GetDataSize(value));
            _cache.Set(key, value, options);
        }

        public CachedItem<string> Get(string key)
        {
            if (_cache.TryGetValue(key, out string value)) return new CachedItem<string>(value);
            return CachedItem<string>.Empty;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        private static long GetDataSize(string data)
        {
            if (string.IsNullOrEmpty(data)) return 18;
            return 18 + data.Length * 2;
        }
    }
}
