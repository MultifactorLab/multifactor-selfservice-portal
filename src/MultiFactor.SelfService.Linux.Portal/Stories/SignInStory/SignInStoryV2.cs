using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;

/// <summary>
/// New version of SignInStory that delegates logic to IdP.
/// SSP only handles UI and passes data to IdP for processing.
/// </summary>
public class SignInStoryV2
{
    private readonly MultifactorIdpApi _idpApiClient;
    private readonly DataProtection _dataProtection;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly PortalSettings _settings;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<SignInStoryV2> _logger;
    private readonly IApplicationCache _applicationCache;
    private readonly ClaimsProvider _claimsProvider;

    public SignInStoryV2(
        MultifactorIdpApi idpApiClient,
        DataProtection dataProtection,
        SafeHttpContextAccessor contextAccessor,
        PortalSettings settings,
        IApplicationCache applicationCache,
        IStringLocalizer<SharedResource> localizer,
        ILogger<SignInStoryV2> logger,
        ClaimsProvider claimsProvider)
    {
        _idpApiClient = idpApiClient;
        _dataProtection = dataProtection;
        _contextAccessor = contextAccessor;
        _settings = settings;
        _localizer = localizer;
        _logger = logger;
        _applicationCache = applicationCache;
        _claimsProvider = claimsProvider;
    }

    public async Task<IActionResult> ExecuteAsync(LoginViewModel model,
        Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(headers);
        
        // Prepare claims from SSP
        var claims = _claimsProvider.GetClaims();
        var sso = _contextAccessor.SafeGetSsoClaims();
        
        var postbackUrl = model.MyUrl.BuildPostbackUrl();
        var adConnectorBaseUrl = model.MyUrl.BuildAdConnectorBaseUrl();

        // Build request for IdP
        var request = new LoginRequestDto()
        {
            Username = model.UserName.Trim(),
            Password = model.Password.Trim(),
            SamlSessionId = sso.SamlSessionId,
            OidcSessionId = sso.OidcSessionId,
            LoginCompletedCallbackUrl = postbackUrl,
            AdConnectorCallbackBaseUrl = adConnectorBaseUrl,
            AdditionalClaims = claims.ToDictionary(x => x.Key, x => x.Value),
            Settings = BuildSspSettings()
        };

        // Call IdP
        var response = await _idpApiClient.LoginAsync(request, headers);

        // Handle response
        return HandleLoginResponse(response, model);
    }

    private SspSettingsDto BuildSspSettings()
    {
        return new SspSettingsDto
        {
            PreAuthenticationMethod = _settings.PreAuthenticationMethod,
            RequiresUserPrincipalName = _settings.ActiveDirectorySettings.RequiresUserPrincipalName,
            PasswordManagementEnabled = _settings.PasswordManagement?.Enabled ?? false,
            PrivacyMode = _settings.MultiFactorApiSettings.PrivacyModeDescriptor.ToString(),
            NetBiosName = _settings.ActiveDirectorySettings.NetBiosName,
            SignUpGroups = _settings.GroupPolicyPreset.SignUpGroups
        };
    }

    private IActionResult HandleLoginResponse(LoginResponseDto response, LoginViewModel model)
    {
        if (!response.Success)
        {
            _logger.LogDebug("Login failed: {Error}", response.ErrorMessage);
            throw new ModelStateErrorException(
                response.ErrorMessage ?? _localizer.GetString("WrongUserNameOrPassword"));
        }

        // Handle MFA required
        if (response.IsMfaRequired && !string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            _logger.LogDebug("Redirecting user to MFA page");
            return new RedirectResult(response.RedirectUrl, true);
        }

        // Handle SAML bypass
        if (response.IsBypassSaml)
        {
            _logger.LogDebug("Bypass second factor for user '{User}' via SAML", model.UserName);
            var sso = _contextAccessor.SafeGetSsoClaims();
            return new RedirectToActionResult("ByPassSamlSession", "Account",
                new { username = model.UserName, samlSession = sso.SamlSessionId });
        }

        // Handle OIDC bypass
        if (response.IsBypassOidc)
        {
            _logger.LogDebug("Bypass second factor for user '{User}' via OIDC", model.UserName);
            var sso = _contextAccessor.SafeGetSsoClaims();
            return new RedirectToActionResult("ByPassOidcSession", "Account",
                new { oidcSession = sso.OidcSessionId });
        }

        // Handle password change required
        if (response.IsChangePassword)
        {
            _logger.LogInformation("User '{User}' must change password", model.UserName);

            // Cache encrypted password for password change flow
            var encryptedPassword = _dataProtection.Protect(
                model.Password.Trim(),
                Constants.PWD_RENEWAL_PURPOSE);
            _applicationCache.Set(
                ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName),
                model.UserName.Trim());
            _applicationCache.Set(
                ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName),
                encryptedPassword);

            // Redirect to MFA if URL is provided
            if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
            {
                return new RedirectResult(response.RedirectUrl, true);
            }

            return new RedirectToActionResult("Change", "ExpiredPassword", null);
        }

        // Handle access denied
        if (response.IsAccessDenied)
        {
            _logger.LogWarning("Access denied for user '{User}'", model.UserName);
            return new RedirectToActionResult("AccessDenied", "Error", null);
        }

        // Default: redirect to MFA page if URL provided
        if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            return new RedirectResult(response.RedirectUrl, true);
        }

        throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
    }
}

