using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    public class LdapServerInfoFactory
    {
        private readonly PortalSettings _settings;
        private readonly ILogger<LdapServerInfoFactory> _logger;

        public LdapServerInfoFactory(PortalSettings settings, ILogger<LdapServerInfoFactory> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<LdapServerInfo> CreateServerInfoAsync()
        {
            using var conn = LdapConnectionAdapter.CreateAnonymous(_settings.CompanySettings.Domain, _logger);
            var info = await conn.GetServerInfoAsync();
            _logger.LogInformation("Ldap implementation: {impl}", info.Implementation);
            return info;
        }
    }
}
