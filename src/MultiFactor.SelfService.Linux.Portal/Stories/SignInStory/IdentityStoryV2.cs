using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;

/// <summary>
/// New version of IdentityStory that delegates logic to IdP.
/// SSP only handles UI and passes data to IdP for processing.
/// Used for pre-authentication flow (MFA first, then password).
/// </summary>
public class IdentityStoryV2
{
    private readonly IMultifactorIdpApi _idpApiClient;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly PortalSettings _settings;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<IdentityStoryV2> _logger;
    private readonly ClaimsProvider _claimsProvider;

    public IdentityStoryV2(
        IMultifactorIdpApi idpApiClient,
        SafeHttpContextAccessor contextAccessor,
        PortalSettings settings,
        IStringLocalizer<SharedResource> localizer,
        ILogger<IdentityStoryV2> logger,
        ClaimsProvider claimsProvider)
    {
        _idpApiClient = idpApiClient;
        _contextAccessor = contextAccessor;
        _settings = settings;
        _localizer = localizer;
        _logger = logger;
        _claimsProvider = claimsProvider;
    }

    public async Task<IActionResult> ExecuteAsync(IdentityViewModel model, Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(headers);

        // Prepare claims from SSP
        var claims = _claimsProvider.GetClaims();
        var sso = _contextAccessor.SafeGetSsoClaims();

        var postbackUrl = model.MyUrl.BuildPostbackUrl();
        var adConnectorBaseUrl = model.MyUrl.BuildAdConnectorBaseUrl();

        // Build request for IdP
        var request = new IdentityRequestDto
        {
            Username = model.UserName.Trim(),
            SamlSessionId = sso.SamlSessionId,
            OidcSessionId = sso.OidcSessionId,
            LoginCompletedCallbackUrl = postbackUrl,
            AdConnectorCallbackBaseUrl = adConnectorBaseUrl,
            AdditionalClaims = claims.ToDictionary(x => x.Key, x => x.Value),
            Settings = BuildSspSettings()
        };

        // Call IdP
        var response = await _idpApiClient.IdentityAsync(request, headers);

        // Handle response
        return HandleIdentityResponse(response, model);
    }

    private IdentitySspSettingsDto BuildSspSettings()
    {
        return new IdentitySspSettingsDto
        {
            PreAuthenticationMethod = _settings.PreAuthenticationMethod,
            RequiresUserPrincipalName = _settings.ActiveDirectorySettings.RequiresUserPrincipalName,
            UseUpnAsIdentity = _settings.ActiveDirectorySettings.UseUpnAsIdentity,
            PrivacyMode = _settings.MultiFactorApiSettings.PrivacyModeDescriptor.ToString(),
            NetBiosName = _settings.ActiveDirectorySettings.NetBiosName,
            SignUpGroups = _settings.GroupPolicyPreset.SignUpGroups
        };
    }

    private IActionResult HandleIdentityResponse(IdentityResponseDto response, IdentityViewModel model)
    {
        if (!response.Success)
        {
            _logger.LogDebug("Identity verification failed: {Error}", response.ErrorMessage);
            throw new ModelStateErrorException(
                response.ErrorMessage ?? _localizer.GetString("WrongUserNameOrPassword"));
        }

        // Handle MFA required - redirect to MFA page
        if (response.IsMfaRequired && !string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            _logger.LogDebug("Redirecting user '{User}' to MFA page", model.UserName);
            return new RedirectResult(response.RedirectUrl, true);
        }

        // Handle ShowAuthn - user can bypass MFA and enter password directly
        // Return the Authn view so user can enter their password
        if (response.IsShowAuthn)
        {
            var identity = response.Username ?? model.UserName;
            _logger.LogInformation("Bypass second factor for user '{User}', showing password form", identity);

            return new ViewResult
            {
                ViewName = "Authn",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    // Create new model with potentially updated identity (UPN instead of sAMAccountName)
                    Model = new IdentityViewModel
                    {
                        UserName = identity,
                        Password = model.Password,
                        MyUrl = model.MyUrl,
                        AccessToken = model.AccessToken
                    }
                }
            };
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