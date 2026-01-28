using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;

namespace MultiFactor.SelfService.Linux.Portal.Stories.Authenticate;

public class AuthenticateSessionStory
{
    private readonly IMultifactorIdpApi _idpApi;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly ILogger<AuthenticateSessionStory> _logger;

    public AuthenticateSessionStory(
        IMultifactorIdpApi idpApi,
        SafeHttpContextAccessor contextAccessor,
        ILogger<AuthenticateSessionStory> logger)
    {
        _idpApi = idpApi;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Execute(string accessToken)
    {
        ArgumentNullException.ThrowIfNull(accessToken);
        _logger.LogDebug("Received MFA token: {accessToken:l}", accessToken);
        
        var request = new LoginCompletedRequestDto
        {
            AccessToken = accessToken
        };
        
        var response = await _idpApi.LoginCompletedAsync(request, _contextAccessor.HttpContext.GetRequiredHeaders());
        
        return HandleLoginCompletedResponse(response, accessToken);
    }

    private IActionResult HandleLoginCompletedResponse(LoginCompletedResponseDto response, string accessToken)
    {
        if (!response.Success)
        {
            _logger.LogDebug("LoginCompleted failed: {Error}", response.ErrorMessage);
            return new RedirectToActionResult("AccessDenied", "Error", default);
        }
        
        if (response.TokenExpirationDate.HasValue)
        {
            _contextAccessor.HttpContext.Response.Cookies.Append(Constants.COOKIE_NAME, accessToken, new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                Expires = response.TokenExpirationDate.Value
            });
        }
        
        if (response.Action == LoginCompletedAction.BypassSaml && !string.IsNullOrEmpty(response.SamlSessionId))
        {
            _logger.LogDebug("Redirecting to SAML bypass for session '{Session}'", response.SamlSessionId);
            return new RedirectToActionResult("ByPassSamlSession", "Account", new { samlSession = response.SamlSessionId });
        }
        
        if (response.Action == LoginCompletedAction.BypassOidc && !string.IsNullOrEmpty(response.OidcSessionId))
        {
            _logger.LogDebug("Redirecting to OIDC bypass for session '{Session}'", response.OidcSessionId);
            return new RedirectToActionResult("ByPassOidcSession", "Account", new { oidcSession = response.OidcSessionId });
        }
        
        if (response.Action == LoginCompletedAction.ChangePassword)
        {
            _logger.LogDebug("User '{User}' must change password", response.Identity);
            return new RedirectToActionResult("Change", "ExpiredPassword", default);
        }
        
        _logger.LogDebug("User '{User}' authenticated successfully", response.Identity);
        return new RedirectToActionResult("Index", "Home", default);
    }
}
