using System.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly PortalSettings _settings;
        private readonly ApplicationCache _applicationCache;
        public AccountController(PortalSettings settings, ApplicationCache applicationCache)
        {
            _settings = settings;
            _applicationCache = applicationCache;
        }

        public IActionResult Login()
        {
            if (_settings.PreAuthnMode)
            {
                return RedirectToAction("Identity");
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [VerifyCaptcha]
        [ConsumeSsoClaims]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, [FromServices] SignInStory signIn)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                return await signIn.ExecuteAsync(model);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// 2-step user verification: 2fa then AD credentials (first factor)
        /// </summary>
        /// <param name="sso">Model for sso integration. Can be empty.</param>
        /// <param name="requestId">State for continuation user verification.</param>
        /// <returns></returns>
        public ActionResult Identity(string requestId)
        {
            if (_settings.PreAuthnMode)
            {
                return RedirectToAction("Login");
            }

            var identity = _applicationCache.GetIdentity(requestId);
            return !identity.IsEmpty
                ? View("Authn", identity.Value)
                : View(new IdentityViewModel());
        }

        
        [HttpPost]
        [VerifyCaptcha]
        [ConsumeSsoClaims]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Identity(IdentityViewModel model, [FromServices] IdentityStory identityModel)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
        
            try
            {
                return await identityModel.ExecuteAsync(model);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }
        [ConsumeSsoClaims]
        public IActionResult Logout([FromServices] SignOutStory signOut) => signOut.Execute();

        [HttpPost]
        public IActionResult PostbackFromMfa(string accessToken,
            [FromServices] AuthenticateSessionStory authenticateSession)
            => authenticateSession.Execute(accessToken);

        [HttpPost]
        public async Task<IActionResult> ByPassSamlSession(string username, string samlSession,
            [FromServices] MultiFactorApi api)
        {
            var page = await api.CreateSamlBypassRequestAsync(username, samlSession);
            return View(page);
        }
    }
}