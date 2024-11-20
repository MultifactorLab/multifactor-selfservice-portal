using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

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

        public void SetIdentity(string key, IdentityViewModel value)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_config.AbsoluteExpiration)
                .SetSize(GetDataSize(value));
            _cache.Set(key, value, options);
        }
        
        public CachedItem<IdentityViewModel> GetIdentity(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return CachedItem<IdentityViewModel>.Empty;
            return _cache.TryGetValue(key, out IdentityViewModel value) 
                ? new CachedItem<IdentityViewModel>(value) 
                : CachedItem<IdentityViewModel>.Empty;
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
        
        private static long GetDataSize(IdentityViewModel data)
        {
            return 18 + data.AccessToken.Length * 2 + data.UserName.Length * 2;
        }
    }
}
