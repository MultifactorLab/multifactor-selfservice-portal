using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.ChangeValidPasswordStory
{
    public class ChangeValidPasswordStory
    {
        private readonly PortalSettings _settings;
        private readonly ActiveDirectoryPasswordChanger _passwordChanger;
        private readonly TokenClaimsAccessor _claimsAccessor;

        public ChangeValidPasswordStory(PortalSettings settings, ActiveDirectoryPasswordChanger passwordChanger, TokenClaimsAccessor claimsAccessor)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
            _claimsAccessor = claimsAccessor ?? throw new ArgumentNullException(nameof(claimsAccessor));
        }

        public async Task<IActionResult> ExecuteAsync(ChangePasswordViewModel model)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            if (!_settings.EnablePasswordManagement)
            {
                return new RedirectToActionResult("Logout", "Account", new { });
            }
            var username = _claimsAccessor.GetTokenClaims().Identity;
            var res = await _passwordChanger.ChangeValidPasswordAsync(username, model.Password, model.NewPassword);
            if (!res.Success) throw new ModelStateErrorException(res.ErrorReason);
            
            return new LocalRedirectResult("/");
        }
    }
}
