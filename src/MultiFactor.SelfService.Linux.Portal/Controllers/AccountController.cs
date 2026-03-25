using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
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
        private readonly ILogger<AccountController> _logger;

        public AccountController(PortalSettings portalSettings,
            IApplicationCache applicationCache, 
            SafeHttpContextAccessor safeHttpContextAccessor,
            ILogger<AccountController> logger)
        {
            _portalSettings = portalSettings;
            _applicationCache = applicationCache;
            _safeHttpContextAccessor = safeHttpContextAccessor;
            _logger = logger;
        }

        [HttpGet("account")]
        [ConsumeSsoClaims]
        public async Task<IActionResult> Auth([FromServices] LoadIdpProfileStory loadProfile)
        {
            
            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();
            try
            {
                await loadProfile.ExecuteAsync();

                if (sso.HasSamlSession())
                {
                    return new RedirectToActionResult("ByPassSamlSession", "Account",
                        new { samlSession = sso.SamlSessionId });
                }

                if (sso.HasOidcSession())
                {
                    return new RedirectToActionResult("ByPassOidcSession", "Account",
                        new { oidcSession = sso.OidcSessionId });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (UnauthorizedException)
            {
                if (ShouldAttemptKerberos())
                {
                    return RedirectToAction("SsoEntry", "Account", sso);
                }

                return RedirectToLoginOrIdentity(sso);
            }
        }

        [HttpGet("account/sso")]
        [ConsumeSsoClaims]
        public IActionResult SsoEntry()
        {
            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();

            if (!_portalSettings.KerberosSettings.Enabled)
            {
                return RedirectToLoginOrIdentity(sso);
            }

            if (Request.Cookies.ContainsKey(Constants.COOKIE_NAME))
            {
                _logger.LogDebug("SSO skipped - already authenticated");
                ClearKerberosAttemptedCookie();

                return RedirectToLoginOrIdentity(sso);
            }
            
            if (Request.Cookies.ContainsKey(Constants.KERBEROS_ATTEMPTED_COOKIE))
            {
                _logger.LogDebug("Kerberos already attempted, fallback to login");

                return RedirectToLoginOrIdentity(sso);
            }
            
            Response.Cookies.Append(
                Constants.KERBEROS_ATTEMPTED_COOKIE,
                "1",
                new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(2)
                });
            
            return RedirectToAction("Negotiate");
        }
        
        [HttpGet("account/sso/negotiate")]
        [ConsumeSsoClaims]
        public async Task<IActionResult> Negotiate(
            [FromServices] KerberosSignInStory kerberosSignIn)
        {
            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();

            try
            {
                var authResult = await HttpContext.AuthenticateAsync(
                    NegotiateDefaults.AuthenticationScheme);

                if (authResult.None)
                {
                    return Challenge(NegotiateDefaults.AuthenticationScheme);
                }

                if (!authResult.Succeeded || authResult.Principal == null)
                {
                    _logger.LogWarning(
                        "Kerberos authentication failed: {Failure}",
                        authResult.Failure?.Message ?? "unknown");

                    return RedirectToLoginOrIdentity(sso);
                }

                _logger.LogDebug("Kerberos authentication succeeded for {User}",
                    authResult.Principal.Identity?.Name);

                ClearKerberosAttemptedCookie();

                var headers = HttpContext.GetRequiredHeaders();
                headers.Remove("Authorization");

                var postbackUrl = Url.Action(
                    "PostbackFromMfa",
                    "Account",
                    null,
                    Request.Scheme)!;

                return await kerberosSignIn.ExecuteAsync(
                    authResult.Principal,
                    sso,
                    headers,
                    postbackUrl);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Kerberos authentication failed");

                return RedirectToLoginOrIdentity(sso);
            }
        }
        
        [HttpGet("account/login")]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost("account/login")]
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
        [HttpGet("account/identity")]
        [ConsumeSsoClaims]
        public async Task<ActionResult> Identity(string requestId, [FromServices] LoadIdpProfileStory loadProfile)
        {
            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();
            try
            {
                await loadProfile.ExecuteAsync();

                if (sso.HasSamlSession())
                {
                    return new RedirectToActionResult("ByPassSamlSession", "Account",
                        new { samlSession = sso.SamlSessionId });
                }

                if (sso.HasOidcSession())
                {
                    return new RedirectToActionResult("ByPassOidcSession", "Account",
                        new { oidcSession = sso.OidcSessionId });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (UnauthorizedException ex)
            {
                if (!_portalSettings.PreAuthenticationMethod)
                {
                    return RedirectToAction("Login", _safeHttpContextAccessor.SafeGetSsoClaims());
                }

                var identity = _applicationCache.GetIdentity(requestId);
                return !identity.IsEmpty
                    ? View("Authn", identity.Value)
                    : View(new IdentityViewModel());
            }
        }

        [HttpPost("account/identity")]
        [VerifyCaptcha]
        [ConsumeSsoClaims]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Identity(IdentityViewModel model,
            [FromServices] IdentityStory identityStoryHandler)
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

        [HttpPost("account/authn")]
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
            await signOut.ExecuteAsync(headers);

            return RedirectToLoginOrIdentity(new SingleSignOnDto(null, null));
        }

        [HttpPost]
        public async Task<IActionResult> PostbackFromMfa(string accessToken,
            [FromServices] AuthenticateSessionStory authenticateSession,
            [FromServices] RedirectToCredValidationAfter2FaStory redirectToCredValidationAfter2faStory)
        {
            if (_portalSettings.PreAuthenticationMethod)
            {
                var authMethod = new JwtSecurityTokenHandler().ReadJwtToken(accessToken)
                    .Claims.FirstOrDefault(c => c.Type == Constants.AuthenticationClaims.AUTHENTICATION_METHODS_REFERENCES)?.Value;

                if (authMethod != Constants.AuthenticationClaims.KERBEROS_METHOD)
                {
                    return await redirectToCredValidationAfter2faStory.ExecuteAsync(accessToken);
                }
            }

            return await authenticateSession.Execute(accessToken);
        }

        [HttpGet]
        public async Task<IActionResult> ByPassSsoSession(string callbackUrl, string accessToken)
        {
            var page = new BypassPageDto(callbackUrl, accessToken);
            return View(page);
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
            catch (UnauthorizedException)
            {
                return RedirectToLoginOrIdentity(_safeHttpContextAccessor.SafeGetSsoClaims());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SAML bypass failed for session '{Session}'", samlSession);
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
            catch (UnauthorizedException)
            {
                return RedirectToLoginOrIdentity(_safeHttpContextAccessor.SafeGetSsoClaims());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OIDC bypass failed for session '{Session}'", oidcSession);
                return RedirectToAction("AccessDenied", "Error");
            }
        }
        
        private bool ShouldAttemptKerberos()
        {
            return _portalSettings.KerberosSettings.Enabled
                   && !Request.Cookies.ContainsKey(Constants.KERBEROS_ATTEMPTED_COOKIE);
        }

        private IActionResult RedirectToLoginOrIdentity(SingleSignOnDto sso)
        {
            return _portalSettings.PreAuthenticationMethod
                ? RedirectToAction("Identity", sso)
                : RedirectToAction("Login", sso);
        }

        private void ClearKerberosAttemptedCookie()
        {
            Response.Cookies.Delete(Constants.KERBEROS_ATTEMPTED_COOKIE);
        }
    }
}