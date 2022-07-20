using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Services.Api.Dto;
using System.Net;

namespace MultiFactor.SelfService.Linux.Portal.Services.Api
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
            _logger.LogInformation("Inside MultiFactorSelfServiceApiClient !");

            try
            {
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
                    true,
                    true);
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
