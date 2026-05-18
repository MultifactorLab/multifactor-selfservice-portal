using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.ChangeValidPassword
{
    public class ChangeValidPasswordStory
    {
        private readonly PortalSettings _settings;
        private readonly UserPasswordChanger _passwordChanger;
        private readonly SafeHttpContextAccessor _contextAccessor;

        public ChangeValidPasswordStory(PortalSettings settings, UserPasswordChanger passwordChanger, TokenClaimsAccessor claimsAccessor, SafeHttpContextAccessor contextAccessor)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _passwordChanger = passwordChanger ?? throw new ArgumentNullException(nameof(passwordChanger));
            _contextAccessor = contextAccessor;
        }

        public async Task<IActionResult> ExecuteAsync(ChangePasswordViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (!_settings.PasswordManagement.Enabled)
            {
                return new RedirectToActionResult("Logout", "Account", new { });
            }
            
            var rawUserName = _contextAccessor.HttpContext.GetRawUserName();
            if (string.IsNullOrWhiteSpace(rawUserName))
            {
                throw new UnauthorizedException("Invalid user");
            }

            var res = await _passwordChanger.ChangePassword(
                rawUserName,
                model.Password,
                model.NewPassword,
                _settings.PasswordManagement.ChangeValidPasswordMode);

            if (!res.Success) throw new ModelStateErrorException(res.ErrorReason);
            
            return new LocalRedirectResult("/Password/Done");
        }
    }
}
