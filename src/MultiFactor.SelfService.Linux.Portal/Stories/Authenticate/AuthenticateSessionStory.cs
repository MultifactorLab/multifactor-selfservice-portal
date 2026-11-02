using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Stories.Authenticate;

public class AuthenticateSessionStory
{
    private readonly IMultifactorIdpApi _idpApi;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly ILogger<AuthenticateSessionStory> _logger;
    private readonly PortalSettings _settings;
    private readonly IMultiFactorApi _api;
    private readonly TokenVerifier _tokenVerifier;

    public AuthenticateSessionStory(
        IMultifactorIdpApi idpApi,
        SafeHttpContextAccessor contextAccessor,
        ILogger<AuthenticateSessionStory> logger,
        PortalSettings settings,
        IMultiFactorApi api,
        TokenVerifier tokenVerifier)
    {
        _idpApi = idpApi;
        _contextAccessor = contextAccessor;
        _logger = logger;
        _settings = settings;
        _api = api;
        _tokenVerifier = tokenVerifier;
    }

    public Task<IActionResult> Execute(string accessToken)
        => ExecuteAsync(accessToken, false, new RedirectToActionResult("Index", "Home", default),
            new RedirectToActionResult("AccessDenied", "Error", default));

    public async Task<IActionResult> ExecutePasswordChangeAsync(string accessToken)
    {
        TokenClaims? original = null;
        try
        {
            original = _tokenVerifier.Verify(accessToken);
            if (!original.MustChangePassword)
            {
                var claims = _tokenVerifier.GetPasswordChangeClaims(accessToken);
                claims[Constants.MultiFactorClaims.ChangePassword] = "true";
                var refreshedToken = await _api.RefreshAccessTokenAsync(accessToken, claims);
                var refreshed = _tokenVerifier.Verify(refreshedToken);
                if (!refreshed.MustChangePassword
                    || refreshed.Identity != original.Identity
                    || refreshed.Id != original.Id
                    || refreshed.RawUserName != (original.RawUserName ?? original.Identity)
                    || refreshed.SamlClaim != original.SamlClaim
                    || refreshed.OidcClaim != original.OidcClaim)
                {
                    throw new InvalidOperationException("Refreshed token has unexpected claims.");
                }

                accessToken = refreshedToken;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to prepare mandatory password change");
            _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
            return new RedirectToActionResult("Login", "Account",
                new { samlSessionId = original?.SamlClaim, oidcSessionId = original?.OidcClaim });
        }

        return await ExecuteAsync(accessToken, true, new RedirectToActionResult("Index", "Home", default),
            new RedirectToActionResult("AccessDenied", "Error", default));
    }

    public Task<IActionResult> ExecuteAfterPasswordChangeAsync(string accessToken, string? samlSessionId = null, string? oidcSessionId = null)
        => ExecuteAsync(accessToken, false, new RedirectToActionResult("Done", "ExpiredPassword", default),
            new RedirectToActionResult("Login", "Account", new { samlSessionId, oidcSessionId }));

    private async Task<IActionResult> ExecuteAsync(string accessToken, bool forcePasswordChange,
        IActionResult authenticatedResult, IActionResult errorResult)
    {
        ArgumentNullException.ThrowIfNull(accessToken);
        _logger.LogDebug("Received MFA token: {accessToken:l}", accessToken);
        
        var request = new LoginCompletedRequestDto
        {
            AccessToken = accessToken
        };
        
        var response = await _idpApi.LoginCompletedAsync(request, _contextAccessor.HttpContext.GetRequiredHeaders());
        
        return HandleLoginCompletedResponse(response, accessToken, forcePasswordChange, authenticatedResult, errorResult);
    }

    private IActionResult HandleLoginCompletedResponse(LoginCompletedResponseDto response, string accessToken,
        bool forcePasswordChange, IActionResult authenticatedResult, IActionResult errorResult)
    {
        if (!response.Success)
        {
            _logger.LogDebug("LoginCompleted failed: {Error}", response.ErrorMessage);
            return response.Action == LoginCompletedAction.Error
                ? errorResult
                : new RedirectToActionResult("AccessDenied", "Error", default);
        }

        var passwordChangeRequired = forcePasswordChange || response.Action == LoginCompletedAction.ChangePassword;
        if (passwordChangeRequired && !_settings.PasswordManagement.Enabled)
        {
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
        
        if (passwordChangeRequired)
        {
            _logger.LogDebug("User '{User}' must change password", response.Identity);
            return new RedirectToActionResult("Change", "ExpiredPassword", default);
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
        
        _logger.LogDebug("User '{User}' authenticated successfully", response.Identity);
        return authenticatedResult;
    }
}
