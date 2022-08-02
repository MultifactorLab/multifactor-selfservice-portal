using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync.Models;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SearchExchangeActiveSyncDevicesStory
{
    public class SearchExchangeActiveSyncDevicesStory
    {
        private readonly TokenClaimsAccessor _claimsAccessor;
        private readonly ExchangeActiveSyncDevicesSearcher _searcher;

        public SearchExchangeActiveSyncDevicesStory(TokenClaimsAccessor claimsAccessor, ExchangeActiveSyncDevicesSearcher searcher)
        {
            _claimsAccessor = claimsAccessor;
            _searcher = searcher;
        }

        public Task<IReadOnlyList<ExchangeActiveSyncDevice>> ExecuteAsync()
        {
            var tokenClaims = _claimsAccessor.GetTokenClaims();
            return _searcher.FindAllByUserAsync(tokenClaims.Identity);
        }
    }
}
