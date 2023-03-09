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
            _portalSettings.AdditionalClaims.EnsureArraysBinging();
            return _portalSettings.AdditionalClaims.Claims
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .Select(x => AdditionalClaimDescriptorFactory.Create(x))
                .ToList()
                .AsReadOnly();
        }
    }
}
