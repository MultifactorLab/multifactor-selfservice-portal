using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Core.Caching
{
    public interface IApplicationCache
    {
        void Set(string key, string value);
        CachedItem<string> Get(string key);
        void SetIdentity(string key, IdentityViewModel value);
        CachedItem<IdentityViewModel> GetIdentity(string key);
        void Remove(string key);
        void SetSupportInfo(string key, SupportViewModel value);
        CachedItem<SupportViewModel> GetSupportInfo(string key);
    }
}
