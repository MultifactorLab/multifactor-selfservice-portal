using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Enums;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignIn;

public class KerberosSignInStory
{
    private readonly IMultifactorIdpApi _idpApiClient;
    private readonly PortalSettings _settings;
    private readonly ILogger<KerberosSignInStory> _logger;
    private readonly ClaimsProvider _claimsProvider;
    private readonly ICredentialVerifier _credentialVerifier;

    public KerberosSignInStory(
        IMultifactorIdpApi idpApiClient,
        PortalSettings settings,
        ILogger<KerberosSignInStory> logger,
        ClaimsProvider claimsProvider,
        ICredentialVerifier credentialVerifier)
    {
        _idpApiClient = idpApiClient;
        _settings = settings;
        _logger = logger;
        _claimsProvider = claimsProvider;
        _credentialVerifier = credentialVerifier;
    }

    public async Task<IActionResult> ExecuteAsync(
        ClaimsPrincipal kerberosPrincipal,
        SingleSignOnDto sso,
        Dictionary<string, string> headers,
        string postbackUrl)
    {
        var username = ExtractUsername(kerberosPrincipal);
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("Could not extract username from Kerberos principal");
            return new RedirectToActionResult("Login", "Account", null);
        }

        var serviceUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User!);
        var parsedUser = LdapIdentity.ParseUser(username);
        if (parsedUser.IsEquivalentTo(serviceUser))
        {
            _logger.LogWarning("Kerberos login attempt as technical account user '{User}'", username);
            return new RedirectToActionResult("Login", "Account", null);
        }

        _logger.LogInformation("Kerberos authentication succeeded for '{User}', verifying membership", username);

        var credentialResult = await _credentialVerifier.VerifyMembership(username);
        if (!credentialResult.IsAuthenticated && !credentialResult.IsBypass && !credentialResult.UserMustChangePassword)
        {
            _logger.LogWarning("Membership verification failed for Kerberos user '{User}': {Reason}",
                username, credentialResult.Reason);
            return new RedirectToActionResult("Login", "Account", null);
        }

        _logger.LogInformation("Kerberos user '{User}' membership verified successfully", username);

        var claims = new Dictionary<string, string>(_claimsProvider.GetClaims())
        {
            { Constants.AuthenticationClaims.AUTHENTICATION_METHODS_REFERENCES, Constants.AuthenticationClaims.KERBEROS_METHOD}
        };
        
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

        return HandleLoginResponse(response, username, sso);
    }

    private IActionResult HandleLoginResponse(
        LoginResponseDto response,
        string username,
        SingleSignOnDto sso)
    {
        if (!response.Success)
        {
            _logger.LogWarning("IDP login failed for Kerberos user '{User}': {Error}", username, response.ErrorMessage);
            return new RedirectToActionResult("Login", "Account", null);
        }

        if (response.Action == LoginAction.MfaRequired && !string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            _logger.LogDebug("Redirecting Kerberos user '{User}' to MFA page", username);
            return new RedirectResult(response.RedirectUrl, true);
        }

        if (response.Action == LoginAction.BypassSaml)
        {
            _logger.LogDebug("Bypass second factor for Kerberos user '{User}' via SAML", username);
            return new RedirectToActionResult("ByPassSamlSession", "Account",
                new { samlSession = sso.SamlSessionId });
        }

        if (response.Action == LoginAction.BypassOidc)
        {
            _logger.LogDebug("Bypass second factor for Kerberos user '{User}' via OIDC", username);
            return new RedirectToActionResult("ByPassOidcSession", "Account",
                new { oidcSession = sso.OidcSessionId });
        }

        if (response.Action == LoginAction.ChangePassword)
        {
            _logger.LogInformation("Kerberos user '{User}' must change password, redirecting to login form", username);
            return new RedirectToActionResult("Login", "Account", null);
        }

        if (response.Action == LoginAction.AccessDenied)
        {
            _logger.LogWarning("Access denied for Kerberos user '{User}'", username);
            return new RedirectToActionResult("AccessDenied", "Error", null);
        }

        if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            return new RedirectResult(response.RedirectUrl, true);
        }

        _logger.LogWarning("Unexpected IDP response for Kerberos user '{User}'", username);
        return new RedirectToActionResult("Login", "Account", null);
    }

    private SspSettingsDto BuildSspSettings()
    {
        return new SspSettingsDto
        {
            RequiresUserPrincipalName = _settings.ActiveDirectorySettings.RequiresUserPrincipalName,
            PasswordManagementEnabled = _settings.PasswordManagement?.Enabled ?? false,
            PrivacyMode = _settings.MultiFactorApiSettings.PrivacyModeDescriptor.ToString(),
            NetBiosName = _settings.ActiveDirectorySettings.NetBiosName,
            SignUpGroups = _settings.GroupPolicyPreset.SignUpGroups
        };
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

    private static string ExtractUsername(ClaimsPrincipal principal)
    {
        var name = principal.Identity?.Name;
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // Kerberos principal: DOMAIN\user or user@DOMAIN.LOCAL
        // LdapIdentity.ParseUser handles both formats correctly
        return name;
    }
}
