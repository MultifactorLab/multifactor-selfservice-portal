using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.ChangeValidPasswordStory
{
    public class ChangeValidPasswordStory
    {
        private readonly PortalSettings _settings;
        private readonly UserPasswordChanger _passwordChanger;
        private readonly TokenClaimsAccessor _claimsAccessor;

        public ChangeValidPasswordStory(PortalSettings settings, UserPasswordChanger passwordChanger, TokenClaimsAccessor claimsAccessor)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
            _claimsAccessor = claimsAccessor ?? throw new ArgumentNullException(nameof(claimsAccessor));
        }

        public async Task<IActionResult> ExecuteAsync(ChangePasswordViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (!_settings.PasswordManagement.Enabled)
            {
                return new RedirectToActionResult("Logout", "Account", new { });
            }
            var username = _claimsAccessor.GetTokenClaims().RawUserName;

            var res = await _passwordChanger.ChangePassword(
                username,
                model.Password,
                model.NewPassword,
                _settings.PasswordManagement.ChangeValidPasswordMode);

            if (!res.Success) throw new ModelStateErrorException(res.ErrorReason);
            
            return new LocalRedirectResult("/Password/Done");
        }
    }
}
