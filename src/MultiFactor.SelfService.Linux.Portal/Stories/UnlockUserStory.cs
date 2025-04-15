using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories;

public class UnlockUserStory
{
    private readonly ILockAttributeChanger _lockAttributeChanger;
    private readonly TokenVerifier _tokenVerifier;
    private readonly MultiFactorApi _apiClient;
    private readonly PortalSettings _portalSettings;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly CredentialVerifier _credentialVerifier;
    private readonly ILogger<UnlockUserStory> _logger;

    public UnlockUserStory(
        ILockAttributeChanger lockAttributeChanger,
        TokenVerifier tokenVerifier,
        MultiFactorApi apiClient,
        PortalSettings portalSettings,
        IStringLocalizer<SharedResource> localizer,
        CredentialVerifier credentialVerifier,
        ILogger<UnlockUserStory> logger)
    {
        _lockAttributeChanger = lockAttributeChanger;
        _tokenVerifier = tokenVerifier;
        _apiClient = apiClient;
        _portalSettings = portalSettings;
        _localizer = localizer;
        _credentialVerifier = credentialVerifier;
        _logger = logger;
    }

    public async Task<IActionResult> CallSecondFactor(EnterIdentityForm form)
    {
        if (!_portalSettings.PasswordManagement.AllowUserUnlock)
            throw new InvalidOperationException();
        
        if (_portalSettings.ActiveDirectorySettings.RequiresUserPrincipalName)
        {
            // AD requires UPN check
            var userName = LdapIdentity.ParseUser(form.Identity);
            if (userName.Type != IdentityType.UserPrincipalName)
            {
                throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
            }
        }
        
        var identity = form.Identity.Trim();
        if (_portalSettings.ActiveDirectorySettings.UseUpnAsIdentity)
        {
            var adValidationResult = await _credentialVerifier.VerifyMembership(identity);
            identity = adValidationResult.UserPrincipalName;
        }
            
        var callback = form.MyUrl.BuildRelativeUrl("Unlock/Complete", 2);
        try
        {
            var response = await _apiClient.StartUnlockingUser(identity, callback);
            return new RedirectResult(response.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
            throw new ModelStateErrorException(_localizer.GetString("AD.UnableToChangePassword"));
        }
    }

    public async Task<bool> UnlockUser(string identity)
    {
        if (!_portalSettings.PasswordManagement.AllowUserUnlock)
            throw new InvalidOperationException();
        
        if (string.IsNullOrWhiteSpace(identity))
            throw new ArgumentNullException(nameof(identity));

        var result = await _lockAttributeChanger.ChangeLockAttributeValue(identity, 0);
        return result;
    }
}