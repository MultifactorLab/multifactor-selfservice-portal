using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using System.Net;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi
{
    public class MultiFactorSelfServiceApiClient
    {
        private readonly PortalSettings _settings;
        private readonly ILogger<MultiFactorSelfServiceApiClient> _logger;

        public MultiFactorSelfServiceApiClient(PortalSettings settings, ILogger<MultiFactorSelfServiceApiClient> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public UserProfileDto LoadProfile()
        {
            try
            {
                // TODO
                //var result = SendRequest<ApiResponse<UserProfile>>("/self-service", "GET");
                return new UserProfileDto(
                    Guid.NewGuid().ToString(),
                    "AlekseyIdentity",
                    "Aleksey",
                    "a.pashkov@multifactor.ru",
                    new List<UserProfileAuthenticatorDto>(),
                    new List<UserProfileAuthenticatorDto>(),
                    new List<UserProfileAuthenticatorDto>(),
                    new List<UserProfileAuthenticatorDto>(),
                    new UserProfilePolicyDto(true, true, true, true),
                    _settings.EnablePasswordManagement,
                    _settings.EnableExchangeActiveSyncDevicesManagement);
            }
            catch (WebException webEx)
            {
                if ((webEx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //invalid or expired token
                    throw new UnauthorizedException();
                }

                _logger.LogError(webEx, $"Unable to connect API {_settings.MultiFactorApiUrl}: {webEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unable to connect API {_settings.MultiFactorApiUrl}: {ex.Message}");
                throw;
            }
        }
    }
}
