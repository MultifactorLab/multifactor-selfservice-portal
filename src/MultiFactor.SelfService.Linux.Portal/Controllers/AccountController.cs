using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly PortalSettings _portalSettings;
        private readonly ApplicationCache _applicationCache;
        private readonly SafeHttpContextAccessor _safeHttpContextAccessor;


        public AccountController(PortalSettings portalSettings,
            ApplicationCache applicationCache, SafeHttpContextAccessor safeHttpContextAccessor)
        {
            _portalSettings = portalSettings;
            _applicationCache = applicationCache;
            _safeHttpContextAccessor = safeHttpContextAccessor;
        }

        [ConsumeSsoClaims]
        public async Task<IActionResult> Login([FromServices] LoadProfileStory loadProfile)
        {
            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();
            try
            {
                if (sso.HasSamlSession())
                {
                    var user = await loadProfile.ExecuteAsync();
                    return new RedirectToActionResult("ByPassSamlSession", "Account", new { username = user.Identity, samlSession = sso.SamlSessionId });
                }

                return View(new LoginViewModel());
            }
            catch (UnauthorizedException ex)
            {
                if (_portalSettings.PreAuthenticationMethod)
                {
                    return RedirectToAction("Identity", sso);
                }

                return View(new LoginViewModel());
            }
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
        [ConsumeSsoClaims]
        public ActionResult Identity(string requestId)
        {
            if (!_portalSettings.PreAuthenticationMethod)
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
        public async Task<IActionResult> Identity(IdentityViewModel model, [FromServices] IdentityStory identityStoryHandler)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                return await identityStoryHandler.ExecuteAsync(model);
            }
            catch (ModelStateErrorException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ConsumeSsoClaims]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authn(IdentityViewModel model, [FromServices] AuthnStory authnStoryHandler)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!_portalSettings.PreAuthenticationMethod)
            {
                return RedirectToAction("Login");
            }

            try
            {
                return await authnStoryHandler.ExecuteAsync(model);
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
            [FromServices] AuthenticateSessionStory authenticateSession,
            [FromServices] RedirectToCredValidationAfter2faStory redirectToCredValidationAfter2FaStory)
        {
            // 2fa before authn enable
            if (_portalSettings.PreAuthenticationMethod)
            {
                // hence continue authentication flow 
                return redirectToCredValidationAfter2FaStory.Execute(accessToken);
            }

            // otherwise flow is (almost) finished
            authenticateSession.Execute(accessToken);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> ByPassSamlSession(string username, string samlSession,
            [FromServices] MultiFactorApi api)
        {
            var page = await api.CreateSamlBypassRequestAsync(username, samlSession);
            return View(page);
        }
    }
}