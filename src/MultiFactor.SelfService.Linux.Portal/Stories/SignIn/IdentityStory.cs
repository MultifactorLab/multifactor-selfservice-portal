using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignIn;

public class IdentityStory
{
    private readonly IMultiFactorApi _multifactorApiClient;
    private readonly IMultifactorIdpApi _idpApiClient;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly PortalSettings _settings;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<IdentityStory> _logger;
    private readonly ClaimsProvider _claimsProvider;
    private readonly ICredentialVerifier _credentialVerifier;

    public IdentityStory(
        IMultiFactorApi multifactorApiClient,
        IMultifactorIdpApi idpApiClient,
        SafeHttpContextAccessor contextAccessor,
        PortalSettings settings,
        IStringLocalizer<SharedResource> localizer,
        ILogger<IdentityStory> logger,
        ClaimsProvider claimsProvider,
        ICredentialVerifier credentialVerifier)
    {
        _multifactorApiClient = multifactorApiClient;
        _idpApiClient = idpApiClient;
        _contextAccessor = contextAccessor;
        _settings = settings;
        _localizer = localizer;
        _logger = logger;
        _claimsProvider = claimsProvider;
        _credentialVerifier = credentialVerifier;
    }

    public async Task<IActionResult> ExecuteAsync(IdentityViewModel model, Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(headers);

        var username = model.UserName.Trim();

        // Validate username format if UPN is required
        if (_settings.ActiveDirectorySettings.RequiresUserPrincipalName && !IsUserPrincipalName(username))
        {
            _logger.LogWarning("UPN format required but not provided for user input");
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }

        // Verify technical account protection
        var serviceUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User!);
        var userName = LdapIdentity.ParseUser(username);
        if (userName.IsEquivalentTo(serviceUser))
        {
            _logger.LogWarning("Attempt to use identity as technical account user '{User}'", username);
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }

        VerifiedMembershipDto verifiedMembership = null;
        var verifiedUsername = default(string);
        // Verify membership locally if prebind info is needed
        if (_settings.NeedPrebindInfo())
        {
            _logger.LogDebug("Verifying membership locally for user '{User}'", username);
            var membershipResult = await _credentialVerifier.VerifyMembership(username);
            verifiedUsername = membershipResult.Username;

            if (!membershipResult.IsAuthenticated)
            {
                _logger.LogWarning("Membership verification failed for user '{User}': {Reason}", username, membershipResult.Reason);
                throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
            }

            _logger.LogInformation("User '{User}' membership verified successfully", username);
            verifiedMembership = MapToVerifiedMembershipDto(membershipResult);
        }

        var authenticators = await _multifactorApiClient.GetUserAuthenticatorsAsync(username);
        if (!authenticators.GetAuthenticators().Any())
        {
            return new ViewResult
            {
                ViewName = "Login",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new LoginViewModel()
                    {
                        UserName = username
                    }

                }
            };
        }

        var claims = new Dictionary<string, string>(_claimsProvider.GetClaims())
        {
            { Constants.AuthenticationClaims.AUTHENTICATION_METHODS_REFERENCES, Constants.AuthenticationClaims.PASSWORD_METHOD }
        };
        
        var sso = _contextAccessor.SafeGetSsoClaims();
        var postbackUrl = model.MyUrl.BuildPostbackUrl();

        var request = new IdentityRequestDto
        {
            Username = verifiedUsername,
            VerifiedMembership = verifiedMembership,
            SamlSessionId = sso.SamlSessionId,
            OidcSessionId = sso.OidcSessionId,
            LoginCompletedCallbackUrl = postbackUrl,
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

    private static VerifiedMembershipDto MapToVerifiedMembershipDto(CredentialVerificationResult result)
    {
        return new VerifiedMembershipDto
        {
            IsBypass = result.IsBypass,
            DisplayName = result.DisplayName,
            Email = result.Email,
            Phone = result.Phone,
            UserPrincipalName = result.UserPrincipalName,
            CustomIdentity = result.CustomIdentity
        };
    }

    private static bool IsUserPrincipalName(string username)
    {
        return username.Contains('@');
    }
}
