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
            try
            {
                using var conn = LdapConnectionAdapter.CreateAnonymous(_settings.CompanySettings.Domain, 
                    x => x.SetLogger(_logger));
                var info = await conn.GetServerInfoAsync();
                _logger.LogInformation("Ldap implementation: {impl}", info);
                return info;
            }
            catch (Exception ex)
            {
                var info = LdapServerInfo.Default;
                _logger.LogWarning(ex, "Unable to retrieve directory info. Using default info: {def}. Maybe anonymous binding is disabled by policy.", info);
                return info;
            }
        }
    }
}
