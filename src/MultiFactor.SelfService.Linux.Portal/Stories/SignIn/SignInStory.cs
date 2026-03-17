using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignIn;

public class SignInStory
{
    private readonly IMultifactorIdpApi _idpApiClient;
    private readonly IMultiFactorApi _apiClient;
    private readonly DataProtection _dataProtection;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly PortalSettings _settings;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<SignInStory> _logger;
    private readonly IApplicationCache _applicationCache;
    private readonly ClaimsProvider _claimsProvider;
    private readonly CredentialVerifier _credentialVerifier;

    public SignInStory(
        IMultifactorIdpApi idpApiClient,
        IMultiFactorApi apiClient,
        DataProtection dataProtection,
        SafeHttpContextAccessor contextAccessor,
        PortalSettings settings,
        IApplicationCache applicationCache,
        IStringLocalizer<SharedResource> localizer,
        ILogger<SignInStory> logger,
        ClaimsProvider claimsProvider,
        CredentialVerifier credentialVerifier)
    {
        _idpApiClient = idpApiClient;
        _apiClient = apiClient;
        _dataProtection = dataProtection;
        _contextAccessor = contextAccessor;
        _settings = settings;
        _localizer = localizer;
        _logger = logger;
        _applicationCache = applicationCache;
        _claimsProvider = claimsProvider;
        _credentialVerifier = credentialVerifier;
    }

    public async Task<IActionResult> ExecuteAsync(LoginViewModel model,
        Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(headers);
        
        var username = model.UserName.Trim();
        var password = model.Password.Trim();
        
        if (_settings.ActiveDirectorySettings.RequiresUserPrincipalName && !IsUserPrincipalName(username))
        {
            _logger.LogWarning("UPN format required but not provided for user input");
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }
        
        var serviceUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User!);
        var userName = LdapIdentity.ParseUser(username);
        if (userName.IsEquivalentTo(serviceUser))
        {
            _logger.LogWarning("Attempt to login as technical account user '{User}'", username);
            await DelayedFailureAsync();
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }
        
        _logger.LogDebug("Verifying credentials locally for user '{User}'", username);
        var credentialResult = await _credentialVerifier.VerifyCredentialAsync(username, password);

        if (!credentialResult.IsAuthenticated && !credentialResult.UserMustChangePassword)
        {
            _logger.LogWarning("Credential verification failed for user '{User}': {Reason}", username, credentialResult.Reason);
            await DelayedFailureAsync();
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }

        _logger.LogInformation("User '{User}' credentials verified successfully", username);

        var claims = _claimsProvider.GetClaims();
        var sso = _contextAccessor.SafeGetSsoClaims();
        var postbackUrl = model.MyUrl.BuildPostbackUrl();
        
        var request = new LoginRequestDto
        {
            VerifiedCredentials = MapToVerifiedCredentialsDto(credentialResult),
            SamlSessionId = sso.SamlSessionId,
            OidcSessionId = sso.OidcSessionId,
            LoginCompletedCallbackUrl = postbackUrl,
            AdditionalClaims = claims.ToDictionary(x => x.Key, x => x.Value),
            Settings = BuildSspSettings()
        };
        
        var response = await _idpApiClient.LoginAsync(request, headers);
        
        return await HandleLoginResponse(response, model, credentialResult);
    }

    private SspSettingsDto BuildSspSettings()
    {
        return new SspSettingsDto
        {
            PreAuthenticationMethod = _settings.PreAuthenticationMethod,
            RequiresUserPrincipalName = _settings.ActiveDirectorySettings.RequiresUserPrincipalName,
            PasswordManagementEnabled = _settings.PasswordManagement?.Enabled ?? false,
            NeedPrebindInfo = _settings.NeedPrebindInfo(),
            PrivacyMode = _settings.MultiFactorApiSettings.PrivacyModeDescriptor.ToString(),
            NetBiosName = _settings.ActiveDirectorySettings.NetBiosName,
            SignUpGroups = _settings.GroupPolicyPreset.SignUpGroups
        };
    }

    private async Task<IActionResult> HandleLoginResponse(LoginResponseDto response, LoginViewModel model, CredentialVerificationResult adValidationResult)
    {
        if (!response.Success)
        {
            _logger.LogDebug("Login failed: {Error}", response.ErrorMessage);
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }
        
        if (response.Action == LoginAction.MfaRequired && !string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            if (_settings.PreAuthenticationMethod)
            {
                _applicationCache.SetPreauthenticationAuthn(
                    ApplicationCacheKeyFactory.CreatePreAuthenticationAuthnSucceedKey(adValidationResult.Username),
                    true);
            }

            _logger.LogDebug("Redirecting user to MFA page");
            return new RedirectResult(response.RedirectUrl, true);
        }
        
        if (response.Action == LoginAction.BypassSaml)
        {
            _logger.LogDebug("Bypass second factor for user '{User}' via SAML", model.UserName);
            
            var userIdentity = GetIdentity(adValidationResult);
            
            var sso = _contextAccessor.SafeGetSsoClaims();
            var user = new UserProfileDto(string.Empty, userIdentity)
            {
                Email = adValidationResult.Email,
                Name = adValidationResult.DisplayName,
            };
            
            var page = await _apiClient.CreateSamlBypassRequestAsync(user, sso.SamlSessionId);

            return new RedirectToActionResult("ByPassSsoSession", "Account",
                new { callbackUrl = page.CallbackUrl, accessToken = page.AccessToken });
        }
        
        if (response.Action == LoginAction.BypassOidc)
        {
            _logger.LogDebug("Bypass second factor for user '{User}' via OIDC", model.UserName);
            var sso = _contextAccessor.SafeGetSsoClaims();
            return new RedirectToActionResult("ByPassOidcSession", "Account",
                new { oidcSession = sso.OidcSessionId });
        }
        
        if (response.Action == LoginAction.ChangePassword)
        {
            _logger.LogInformation("User '{User}' must change password", model.UserName);
            
            var encryptedPassword = _dataProtection.Protect(
                model.Password.Trim(),
                Constants.PWD_RENEWAL_PURPOSE);
            _applicationCache.Set(
                ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName),
                model.UserName.Trim());
            _applicationCache.Set(
                ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName),
                encryptedPassword);
            
            if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
            {
                return new RedirectResult(response.RedirectUrl, true);
            }

            return new RedirectToActionResult("Change", "ExpiredPassword", null);
        }
        
        if (response.Action == LoginAction.AccessDenied)
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

    private static VerifiedCredentialsDto MapToVerifiedCredentialsDto(CredentialVerificationResult result)
    {
        return new VerifiedCredentialsDto
        {
            IsAuthenticated = result.IsAuthenticated,
            IsBypass = result.IsBypass,
            UserMustChangePassword = result.UserMustChangePassword,
            PasswordExpirationDate = result.PasswordExpirationDate,
            DisplayName = result.DisplayName,
            Email = result.Email,
            Phone = result.Phone,
            Username = result.Username,
            UserPrincipalName = result.UserPrincipalName,
            CustomIdentity = result.CustomIdentity,
            Reason = result.Reason
        };
    }

    private static bool IsUserPrincipalName(string username)
    {
        return username.Contains('@');
    }

    private static async Task DelayedFailureAsync()
    {
        var rnd = new Random();
        var delay = rnd.Next(2, 6);
        await Task.Delay(TimeSpan.FromSeconds(delay));
    }
    
    private string GetIdentity(CredentialVerificationResult verificationResult)
    {
        return !string.IsNullOrWhiteSpace(verificationResult.CustomIdentity)
            ? verificationResult.CustomIdentity
            : verificationResult.Username;
    }
}
