using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Models;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoginStory
{
    public class LoginStory
    {
        private readonly ActiveDirectoryCredentialVerifier _credentialVerifier;
        private readonly DataProtection _dataProtection;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer<Login> _localizer;

        public LoginStory(ActiveDirectoryCredentialVerifier credentialVerifier, DataProtection dataProtection, PortalSettings settings, IStringLocalizer<Login> localizer)
        {
            _credentialVerifier = credentialVerifier;
            _dataProtection = dataProtection;
            _settings = settings;
            _localizer = localizer;
        }

        public async Task<IActionResult> ExecuteAsync(LoginModel model)
        {
            if (_settings.RequiresUserPrincipalName)
            {
                // AD requires UPN check
                var userName = LdapIdentity.ParseUser(model.UserName);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
                }
            }

            // AD credential check
            var adValidationResult = await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());

            //if (adValidationResult.UserMustChangePassword && _settings.EnablePasswordManagement)
            //{
            //    var encryptedPassword = _dataProtection.Protect(model.Password.Trim());
            //    Session[Constants.SESSION_EXPIRED_PASSWORD_USER_KEY] = model.UserName.Trim();
            //    Session[Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY] = encryptedPassword;

            //    return new RedirectToActionResult("Change", "ExpiredPassword", new { });
            //}

            // Invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));

            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }
    }
}
