using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync.Models;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync
{
    public class DeviceAccessStateNameLocalizer
    {
        private const string _prefix = "SyncDeviceAccessState";
        private readonly ExchangeActiveSyncDeviceAccessState[] _validStates =
        {
            ExchangeActiveSyncDeviceAccessState.Allowed,
            ExchangeActiveSyncDeviceAccessState.Blocked,
            ExchangeActiveSyncDeviceAccessState.Quarantined,
            ExchangeActiveSyncDeviceAccessState.TestActiveSyncConnectivity
        };

        private readonly IStringLocalizer _localizer;

        public DeviceAccessStateNameLocalizer(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public string GetLocalizedStateName(ExchangeActiveSyncDeviceAccessState state)
        {
            if (!_validStates.Contains(state)) throw new NotImplementedException(state.ToString());
            return _localizer.GetString($"{_prefix}{state}");
        }
    }
}
