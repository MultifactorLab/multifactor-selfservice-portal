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
using MultiFactor.SelfService.Linux.Portal.Helpers;
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
                if (!AccountFlowHelper.ShouldAttemptKerberos(_portalSettings, Request))
                {
                    return RedirectToLoginOrIdentity(sso);
                }

                return RedirectToAction("SsoEntry",
                    "Account",
                    AccountFlowHelper.ToRouteValues(sso, 0, Guid.NewGuid().ToString("N")));
            }
        }

        [HttpGet("account/sso")]
        [ConsumeSsoClaims]
        public IActionResult SsoEntry([FromQuery] int attempt = 0, [FromQuery] string? flowId = null)
        {
            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();

            flowId ??= Guid.NewGuid().ToString("N");

            _logger.LogDebug("SSO entry: attempt={Attempt}, flowId={FlowId}", attempt, flowId);

            if (!_portalSettings.KerberosSettings.Enabled || User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLoginOrIdentity(sso);
            }

            if (attempt > 0)
            {
                _logger.LogDebug("SSO fallback triggered: flowId={FlowId}", flowId);
                return RedirectToLoginOrIdentity(sso);
            }

            var negotiateUrl = Url.Action(
                "Negotiate",
                "Account",
                AccountFlowHelper.BuildSsoRouteValues(sso, attempt: 0, flowId))!;

            var fallbackUrl = Url.Action(
                "SsoEntry",
                "Account",
                AccountFlowHelper.BuildSsoRouteValues(sso, attempt: 1, flowId))!;

            ViewBag.NegotiateUrl = negotiateUrl;
            ViewBag.FallbackUrl = fallbackUrl;

            return View("SsoEntry");
        }
        
        [HttpGet("account/sso/negotiate")]
        [ConsumeSsoClaims]
        public async Task<IActionResult> Negotiate([FromServices] KerberosSignInStory kerberosSignIn)
        {
            Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();

            try
            {
                if (AccountFlowHelper.IsNtlmToken(Request))
                {
                    _logger.LogDebug("NTLM token detected, rejecting — Kerberos only");
                    return ParentWindowRedirect(RedirectToLoginOrIdentity(sso));
                }

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

                    return ParentWindowRedirect(RedirectToLoginOrIdentity(sso));
                }

                _logger.LogDebug("Kerberos authentication succeeded for {User}",
                    authResult.Principal.Identity?.Name);

                var headers = HttpContext.GetRequiredHeaders();
                headers.Remove("Authorization");

                var postbackUrl = Url.Action(
                    "PostbackFromMfa",
                    "Account",
                    null,
                    Request.Scheme)!;

                var result = await kerberosSignIn.ExecuteAsync(
                    authResult.Principal,
                    sso,
                    headers,
                    postbackUrl);
                
                return ParentWindowRedirect(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Kerberos authentication failed");

                return ParentWindowRedirect(RedirectToLoginOrIdentity(sso));
            }
        }
        
        [HttpGet("account/login")]
        public IActionResult Login()
        {
            var sso = _safeHttpContextAccessor.SafeGetSsoClaims();
            
            if (_portalSettings.PreAuthenticationMethod)
            {
                return RedirectToAction("Identity", sso);
            }

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
        
        private IActionResult RedirectToLoginOrIdentity(SingleSignOnDto sso)
        {
            return _portalSettings.PreAuthenticationMethod
                ? RedirectToAction("Identity", sso)
                : RedirectToAction("Login", sso);
        }
        
        private IActionResult ParentWindowRedirect(IActionResult result)
        {
            var url = AccountFlowHelper.ResolveRedirectUrl(Url, result);
            return url == null ? result : View("ParentWindowRedirect", url);
        }
    }
}