using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;

/// <summary>
/// New version of AuthenticateSessionStory that delegates logic to IdP.
/// SSP only handles UI and passes data to IdP for processing.
/// </summary>
public class AuthenticateSessionStoryV2
{
    private readonly IMultifactorIdpApi _idpApi;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly ILogger<AuthenticateSessionStoryV2> _logger;

    public AuthenticateSessionStoryV2(
        IMultifactorIdpApi idpApi,
        SafeHttpContextAccessor contextAccessor,
        ILogger<AuthenticateSessionStoryV2> logger)
    {
        _idpApi = idpApi;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Execute(string accessToken)
    {
        ArgumentNullException.ThrowIfNull(accessToken);
        _logger.LogDebug("Received MFA token: {accessToken:l}", accessToken);

        // Build request for IdP
        var request = new LoginCompletedRequestDto
        {
            AccessToken = accessToken
        };

        // Call IdP
        var response = await _idpApi.LoginCompletedAsync(request, _contextAccessor.HttpContext.GetRequiredHeaders());

        // Handle response
        return HandleLoginCompletedResponse(response, accessToken);
    }

    private IActionResult HandleLoginCompletedResponse(LoginCompletedResponseDto response, string accessToken)
    {
        // Set cookie with access token
        if (response.TokenExpirationDate.HasValue)
        {
            _contextAccessor.HttpContext.Response.Cookies.Append(Constants.COOKIE_NAME, accessToken, new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                Expires = response.TokenExpirationDate.Value
            });
        }

        // Handle SAML bypass
        if (response.IsBypassSaml && !string.IsNullOrEmpty(response.SamlSessionId))
        {
            _logger.LogDebug("Redirecting to SAML bypass for session '{Session}'", response.SamlSessionId);
            return new RedirectToActionResult("ByPassSamlSession", "Account", new { samlSession = response.SamlSessionId });
        }

        // Handle OIDC bypass
        if (response.IsBypassOidc && !string.IsNullOrEmpty(response.OidcSessionId))
        {
            _logger.LogDebug("Redirecting to OIDC bypass for session '{Session}'", response.OidcSessionId);
            return new RedirectToActionResult("ByPassOidcSession", "Account", new { oidcSession = response.OidcSessionId });
        }

        // Handle password change required
        if (response.IsChangePassword)
        {
            _logger.LogDebug("User '{User}' must change password", response.Identity);
            return new RedirectToActionResult("Change", "ExpiredPassword", default);
        }

        // Standard authenticated flow
        _logger.LogDebug("User '{User}' authenticated successfully", response.Identity);
        return new RedirectToActionResult("Index", "Home", default);
    }
}

