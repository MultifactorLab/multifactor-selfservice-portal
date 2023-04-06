using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.RecoverPasswordStory
{
    public class RecoverPasswordStory
    {
        private readonly MultiFactorApi _apiClient;
        private readonly PortalSettings _portalSettings;
        private readonly ILogger<RecoverPasswordStory> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        public RecoverPasswordStory(
            MultiFactorApi apiClient, 
            PortalSettings portalSettings, 
            ILogger<RecoverPasswordStory> logger,
            IStringLocalizer<SharedResource> localizer)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _portalSettings = portalSettings ?? throw new ArgumentNullException(nameof(portalSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer;
        }

        public async Task<IActionResult> StartRecoverAsync(EnterIdentityForm form)
        {
            if (_portalSettings.RequiresUserPrincipalName)
            {
                // AD requires UPN check
                var userName = LdapIdentity.ParseUser(form.Identity);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    // ModelState.AddModelError(string.Empty, .UserNameUpnRequired);
                    throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
                }
            }
            var callback = BuildCallbackUrl(form.MyUrl, "reset");

            try
            {
                var response = await _apiClient.StartResetPassword(form.Identity.Trim(), callback);
                return new RedirectResult(response.Url);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
                // TempData["reset-password-error"] = Resources.PasswordReset.ErrorMessage;
                throw new ModelStateErrorException(_localizer.GetString("ErrorMessage"));
            }
        }


        public static string BuildCallbackUrl(string currentPath, string relPath, int removeSegments = 0)
        {
            if (currentPath is null) throw new ArgumentNullException(nameof(currentPath));
            if (relPath is null) throw new ArgumentNullException(nameof(relPath));

            // public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(currentPath);
            var noLastSegment = string.Format("{0}://{1}", currentUri.Scheme, currentUri.Authority);

            for (int i = 0; i < currentUri.Segments.Length - removeSegments; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing
            return $"{noLastSegment.Trim("/".ToCharArray())}/{relPath}";
        }
    }
}
