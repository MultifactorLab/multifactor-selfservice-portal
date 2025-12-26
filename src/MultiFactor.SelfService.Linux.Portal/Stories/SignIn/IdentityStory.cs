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
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignIn;

public class IdentityStory
{
    private readonly IMultifactorIdpApi _idpApiClient;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly PortalSettings _settings;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<IdentityStory> _logger;
    private readonly ClaimsProvider _claimsProvider;

    public IdentityStory(
        IMultifactorIdpApi idpApiClient,
        SafeHttpContextAccessor contextAccessor,
        PortalSettings settings,
        IStringLocalizer<SharedResource> localizer,
        ILogger<IdentityStory> logger,
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
        
        var claims = _claimsProvider.GetClaims();
        var sso = _contextAccessor.SafeGetSsoClaims();

        var postbackUrl = model.MyUrl.BuildPostbackUrl();
        var adConnectorBaseUrl = model.MyUrl.BuildAdConnectorBaseUrl();
        
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
        
        var response = await _idpApiClient.IdentityAsync(request, headers);
        
        return HandleIdentityResponse(response, model);
    }

    private IdentitySspSettingsDto BuildSspSettings()
    {
        return new IdentitySspSettingsDto
        {
            PreAuthenticationMethod = _settings.PreAuthenticationMethod,
            RequiresUserPrincipalName = _settings.ActiveDirectorySettings.RequiresUserPrincipalName,
            NeedPrebindInfo = _settings.NeedPrebindInfo(),
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
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }
        
        if (response.Action == IdentityAction.MfaRequired && !string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            _logger.LogDebug("Redirecting user '{User}' to MFA page", model.UserName);
            return new RedirectResult(response.RedirectUrl, true);
        }
        
        if (response.Action == IdentityAction.ShowAuthn)
        {
            var identity = response.Username ?? model.UserName;
            _logger.LogInformation("Bypass second factor for user '{User}', showing password form", identity);

            return new ViewResult
            {
                ViewName = "Authn",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
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
        
        if (response.Action == IdentityAction.AccessDenied)
        {
            _logger.LogWarning("Access denied for user '{User}'", model.UserName);
            return new RedirectToActionResult("AccessDenied", "Error", null);
        }
        
        if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            return new RedirectResult(response.RedirectUrl, true);
        }

        throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
    }
}
