using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.Authenticate;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfile;
using MultiFactor.SelfService.Linux.Portal.Stories.SignIn;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOut;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [AllowAnonymous]
    public class AccountController : ControllerBase
    {
        private readonly PortalSettings _portalSettings;
        private readonly IApplicationCache _applicationCache;
        private readonly SafeHttpContextAccessor _safeHttpContextAccessor;


        public AccountController(PortalSettings portalSettings,
            IApplicationCache applicationCache, SafeHttpContextAccessor safeHttpContextAccessor)
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
                var user = await loadProfile.ExecuteAsync();

                if (sso.HasSamlSession())
                {
                    return new RedirectToActionResult("ByPassSamlSession", "Account", new { samlSession = sso.SamlSessionId });
                }

                if (sso.HasOidcSession())
                {
                    return new RedirectToActionResult("ByPassOidcSession", "Account", new { oidcSession = sso.OidcSessionId });
                }

                return RedirectToAction("Index", "Home");
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
                var headers = HttpContext.GetRequiredHeaders();
                
                return await signIn.ExecuteAsync(model, headers);
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
        public async Task<ActionResult> Identity(string requestId, [FromServices] LoadProfileStory loadProfile)
        {
            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();
            try
            {
                var user = await loadProfile.ExecuteAsync();

                if (sso.HasSamlSession())
                {
                    return new RedirectToActionResult("ByPassSamlSession", "Account", new { samlSession = sso.SamlSessionId });
                }

                if (sso.HasOidcSession())
                {
                    return new RedirectToActionResult("ByPassOidcSession", "Account", new { oidcSession = sso.OidcSessionId });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (UnauthorizedException ex)
            {
                if (!_portalSettings.PreAuthenticationMethod)
                {
                    return RedirectToAction("Login", sso);
                }

                var identity = _applicationCache.GetIdentity(requestId);
                return !identity.IsEmpty
                    ? View("Authn", identity.Value)
                    : View(new IdentityViewModel());
            }
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
                return await identityStoryHandler.ExecuteAsync(model, HttpContext.GetRequiredHeaders());
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
        public async Task<IActionResult> Logout([FromServices] SignOutStory signOut)
        {
            var headers = HttpContext.GetRequiredHeaders();
            return await signOut.ExecuteAsync(headers);
        }

        [HttpPost]
        public async Task<IActionResult> PostbackFromMfa(string accessToken,
            [FromServices] AuthenticateSessionStory authenticateSession,
            [FromServices] RedirectToCredValidationAfter2FaStory redirectToCredValidationAfter2faStory)
        {
            if (_portalSettings.PreAuthenticationMethod)
            {
                return await redirectToCredValidationAfter2faStory.ExecuteAsync(accessToken);
            }
            
            return await authenticateSession.Execute(accessToken);
        }

        [HttpGet]
        public async Task<IActionResult> ByPassSamlSession(
            string samlSession,
            [FromServices] IMultifactorIdpApi idpApi)
        {
            try
            {
                var request = new BypassSamlRequestDto
                {
                    SamlSessionId = samlSession
                };

                var response = await idpApi.BypassSamlAsync(request, HttpContext.GetRequiredHeaders());
                
                if (!string.IsNullOrWhiteSpace(response.SamlResponseHtml))
                {
                    return Content(response.SamlResponseHtml, "text/html");
                }

                return RedirectToAction("AccessDenied", "Error");
            }
            catch (UnauthorizedException ex)
            {
                if (_portalSettings.PreAuthenticationMethod)
                {
                    return RedirectToAction("Identity", _safeHttpContextAccessor.SafeGetSsoClaims());
                }

                return View(new LoginViewModel());
            }
            catch (Exception ex)
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ByPassOidcSession(
            string oidcSession,
            [FromServices] IMultifactorIdpApi idpApi)
        {
            try
            {
                var request = new BypassOidcRequestDto
                {
                    OidcSessionId = oidcSession
                };
                
                var response = await idpApi.BypassOidcAsync(request, HttpContext.GetRequiredHeaders());
                
                if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
                {
                    return Redirect(response.RedirectUrl);
                }

                return RedirectToAction("AccessDenied", "Error");
            }
            catch (UnauthorizedException ex)
            {
                if (_portalSettings.PreAuthenticationMethod)
                {
                    return RedirectToAction("Identity", _safeHttpContextAccessor.SafeGetSsoClaims());
                }

                return View(new LoginViewModel());
            }
            catch (Exception ex)
            {
                return RedirectToAction("AccessDenied", "Error");
            }
        }
    }
}