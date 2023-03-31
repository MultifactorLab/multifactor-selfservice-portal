using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [RequiredFeature(ApplicationFeature.PasswordManagement)]
    public class PasswordRecoveryController : ControllerBase
    {
        private ILogger _logger;
        private PortalSettings _portalSettings;
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        [VerifyCaptcha(CaptchaPlace.PasswordRecovery)]
        [ValidateAntiForgeryToken]
        public ActionResult Index(EnterIdentityForm form)
        {
            if (_portalSettings.RequiresUserPrincipalName)
            {
                // AD requires UPN check
                var userName = LdapIdentity.ParseUser(form.Identity);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    ModelState.AddModelError(string.Empty, ""/*Resources.AccountLogin.UserNameUpnRequired*/);
                    return View(form);
                }
            }
            return View();
            /*
            var callback = ""//CallbackUrlFactory.BuildCallbackUrl(form.MyUrl, "reset");
           
            {
                var response = _apiClient.StartResetPassword(form.Identity.Trim(), callback);
                if (response.Success) return RedirectPermanent(response.Model.Url);

                _logger.Error("Unable to recover password for user '{u:l}': {m:l}", form.Identity, response.Message);
                TempData["reset-password-error"] = Resources.PasswordReset.ErrorMessage;
                return RedirectToAction("Wrong");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to recover password for user '{u:l}': {m:l}", form.Identity, ex.Message);
                TempData["reset-password-error"] = Resources.PasswordReset.ErrorMessage;
                return RedirectToAction("Wrong");
            }*/
        }
    }
}
