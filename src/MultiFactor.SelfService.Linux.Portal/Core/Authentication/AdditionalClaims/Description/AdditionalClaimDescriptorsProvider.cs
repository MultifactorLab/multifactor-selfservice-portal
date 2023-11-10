using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description
{
    public class AdditionalClaimDescriptorsProvider
    {
        private readonly PortalSettings _portalSettings;

        public AdditionalClaimDescriptorsProvider(PortalSettings portalSettings)
        {
            _portalSettings = portalSettings ?? throw new ArgumentNullException(nameof(portalSettings));
        }

        public IReadOnlyList<AdditionalClaimDescriptor> GetDescriptors()
        {
            return _portalSettings.AdditionalClaims.GetClaims()
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .Select(AdditionalClaimDescriptorFactory.Create)
                .ToList()
                .AsReadOnly();
        }
    }
}
