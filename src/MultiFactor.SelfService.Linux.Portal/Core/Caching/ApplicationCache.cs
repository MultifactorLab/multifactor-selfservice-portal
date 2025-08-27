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
            return CalculateStringSize(data);
        }
        
        private static long GetDataSize(IdentityViewModel data)
        {
            return CalculateStringSize(data.AccessToken) +
                   CalculateStringSize(data.UserName);
        }
        
        public void SetSupportInfo(string key, SupportViewModel value)
        {
            var expiration = value.IsEmpty() 
                ? _config.SupportInfoEmptyExpiration 
                : _config.SupportInfoExpiration;
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration)
                .SetSize(GetDataSize(value));
            _cache.Set(key, value, options);
        }

        public CachedItem<SupportViewModel> GetSupportInfo(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return CachedItem<SupportViewModel>.Empty;
            return _cache.TryGetValue(key, out SupportViewModel value) 
                ? new CachedItem<SupportViewModel>(value) 
                : CachedItem<SupportViewModel>.Empty;
        }
        
        private static long GetDataSize(SupportViewModel data)
        {
            return CalculateStringSize(data.AdminEmail) + 
                   CalculateStringSize(data.AdminName) + 
                   CalculateStringSize(data.AdminPhone);
        }
        
        private static long CalculateStringSize(string s)
        {
            var headerSize = IntPtr.Size == 4 ? 14 : 22; // 32-bit ? 14 : 22
            var size = headerSize + 2L * s.Length;
            var alignment = IntPtr.Size == 4 ? 4 : 8; // 32-bit ? 4 : 8
            return (long)Math.Ceiling(size / (double)alignment) * alignment;
        }
    }
}
