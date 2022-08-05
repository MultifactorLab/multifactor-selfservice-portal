using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory.Dto;
using System.Reflection;

namespace MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory
{
    /// <summary>
    /// Returns information about application status.
    /// </summary>
    public class GetApplicationInfoStory
    {
        private readonly MultiFactorApi _api;
        private readonly PortalSettings _settings;
        private readonly IConfiguration _config;
        private readonly ILogger<GetApplicationInfoStory> _logger;

        public GetApplicationInfoStory(MultiFactorApi api, PortalSettings settings, IConfiguration config, ILogger<GetApplicationInfoStory> logger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApplicationInfoDto> ExecuteAsync()
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
            var apiSTatus = await GetApiStatusAsync();
            var ldapStatus = await GetLdapStatus();
            return new ApplicationInfoDto(
                _config.GetEnvironment(),
                DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
                version,
                apiSTatus.ToString(),
                ldapStatus.ToString()
                );
        }

        private async Task<ApplicationComponentStatus> GetApiStatusAsync()
        {
            try
            {
                await _api.PingAsync();
                return ApplicationComponentStatus.Ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while requesting API status");
                return ApplicationComponentStatus.Unreachable;
            }
        }

        private async Task<ApplicationComponentStatus> GetLdapStatus()
        {
            try
            {
                var user = LdapIdentity.ParseUser(_settings.TechnicalAccUsr);
                using var conn = await LdapConnectionAdapter.CreateAsync(_settings.CompanyDomain, user, _settings.TechnicalAccPwd, _logger);
                return ApplicationComponentStatus.Ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while requesting LDAP status");
                return ApplicationComponentStatus.Unreachable;
            }
        }
    }
}
