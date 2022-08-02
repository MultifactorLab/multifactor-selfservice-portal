using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync.Models;

namespace MultiFactor.SelfService.Linux.Portal.Stories.ChangeActiveSyncDeviceStateStory
{
    public class ChangeActiveSyncDeviceStateStory
    {
        private readonly TokenClaimsAccessor _claimsAccessor;
        private readonly ExchangeActiveSyncDevicesSearcher _searcher;
        private readonly ExchangeActiveSyncDeviceStateChanger _stateChanger;
        private readonly ILogger<ChangeActiveSyncDeviceStateStory> _logger;

        public ChangeActiveSyncDeviceStateStory(TokenClaimsAccessor claimsAccessor, 
            ExchangeActiveSyncDevicesSearcher searcher,
            ExchangeActiveSyncDeviceStateChanger stateChanger,
            ILogger<ChangeActiveSyncDeviceStateStory> logger)
        {
            _claimsAccessor = claimsAccessor ?? throw new ArgumentNullException(nameof(claimsAccessor));
            _searcher = searcher ?? throw new ArgumentNullException(nameof(searcher));
            _stateChanger = stateChanger ?? throw new ArgumentNullException(nameof(stateChanger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ApproveAsync(string deviceId)
        {
            if (deviceId is null) throw new ArgumentNullException(nameof(deviceId));
            
            var username = _claimsAccessor.GetTokenClaims().Identity;
            var device = await _searcher.FindByUserAndDeviceIdAsync(username, deviceId);
            if (device == null) return;

            throw new NotImplementedException();

           // _stateChanger.ChangeSatate(ExchangeActiveSyncDeviceAccessState.Allowed);
            //service.UpdateExchangeActiveSyncDeviceState(token.Identity, cn, state);          
        }

        public async Task RejectAsync(string deviceId)
        {
            if (deviceId is null) throw new ArgumentNullException(nameof(deviceId));

            var username = _claimsAccessor.GetTokenClaims().Identity;
            var device = await _searcher.FindByUserAndDeviceIdAsync(username, deviceId);
            if (device == null) return;

            throw new NotImplementedException();

            // _stateChanger.ChangeSatate(ExchangeActiveSyncDeviceAccessState.Blocked);
            //service.UpdateExchangeActiveSyncDeviceState(token.Identity, cn, state); 
        }
    }
}
