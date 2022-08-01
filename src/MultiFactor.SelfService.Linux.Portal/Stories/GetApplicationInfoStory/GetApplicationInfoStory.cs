using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory
{
    public class GetApplicationInfoStory
    {
        private readonly MultiFactorApi _api;
        private readonly ILogger<GetApplicationInfoStory> _logger;

        public GetApplicationInfoStory(MultiFactorApi api, ILogger<GetApplicationInfoStory> logger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApplicationInfoDto> ExecuteAsync()
        {
            var apiSTatus = await GetApiStatusAsync();
            var ldapStatus = await GetLdapStatus();
            return new ApplicationInfoDto(
                DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
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
                // TODO
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
