using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;

public class AuthnStory
{
    private readonly CredentialVerifier _credentialVerifier;
    private readonly DataProtection _dataProtection;
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly PortalSettings _settings;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<SignInStory> _logger;
    private readonly ApplicationCache _applicationCache;
    private readonly AuthenticateSessionStory _authenticateSessionStory;
    private readonly IHttpClientFactory _httpFactory;

    public AuthnStory(CredentialVerifier credentialVerifier,
        DataProtection dataProtection,
        SafeHttpContextAccessor contextAccessor,
        PortalSettings settings,
        ApplicationCache applicationCache,
        IStringLocalizer<SharedResource> localizer,
        ILogger<SignInStory> logger,
        AuthenticateSessionStory authenticateSessionStory, IHttpClientFactory httpFactory)
    {
        _credentialVerifier = credentialVerifier ?? throw new ArgumentNullException(nameof(credentialVerifier));
        _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(applicationCache));
        _authenticateSessionStory = authenticateSessionStory;
        _httpFactory = httpFactory;
    }

    public async Task<IActionResult> ExecuteAsync(IdentityViewModel model)
    {
        var userName = LdapIdentity.ParseUser(model.UserName);
        if (_settings.ActiveDirectorySettings.RequiresUserPrincipalName)
        {
            if (userName.Type != IdentityType.UserPrincipalName)
            {
                throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
            }
        }

        var serviceUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User!);
        if (userName.IsEquivalentTo(serviceUser)) return await WrongAsync();

        
        // authn after 2fa
        // AD credential check
        var adValidationResult = await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());
        
        // credential is VALID
        if (adValidationResult.IsAuthenticated)
        {
            _logger.LogInformation("User '{user}' credential verified successfully in {domain:l}", userName,
                _settings.CompanySettings.Domain);
            var sso = _contextAccessor.SafeGetSsoClaims();
            if (sso.HasSamlSession())
            {
                if (adValidationResult.IsBypass)
                {
                    return new RedirectToActionResult("ByPassSamlSession", "account",
                        new { username = model.UserName, samlSession = sso.SamlSessionId });
                }
                
                // go to idp, return and render html form with saml assertion
                return await GetSamlAssertion(model.AccessToken);
            }

            _authenticateSessionStory.Execute(model.AccessToken);
            return new RedirectToActionResult("Index", "Home", default);
        }

        if (adValidationResult.UserMustChangePassword && _settings.PasswordManagement.Enabled)
        {
            var encryptedPassword = _dataProtection.Protect(model.Password.Trim(), Constants.PWD_RENEWAL_PURPOSE);
            _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdUserKey(model.UserName),
                model.UserName.Trim());
            _applicationCache.Set(ApplicationCacheKeyFactory.CreateExpiredPwdCipherKey(model.UserName),
                encryptedPassword);

            return _authenticateSessionStory.Execute(model.AccessToken);
        }

        return await WrongAsync();
    }

    private async Task<ActionResult> GetSamlAssertion(string accessToken)
    {
        // no token verification because 'aud'=api_key and ssp_api_key!=saml_api_key
        // hence verification will fail. but it's ok, idp service make its own verification
        // so security not broken
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var idpUrl = token.Claims.FirstOrDefault(claim => claim.Type == Constants.MultiFactorClaims.AdditionSsoStep)
            ?.Value;

        try
        {
            MultipartFormDataContent multipartContent = new MultipartFormDataContent
            {
                {
                    new StringContent(accessToken, Encoding.UTF8, MediaTypeNames.Text.Plain),
                    "accessToken"
                }
            };
            HttpClient httpClient = _httpFactory.CreateClient();
            var res = await httpClient.PostAsync(idpUrl, multipartContent);
            var jsonResponse = await res.Content.ReadAsStringAsync();
            
            // must render idp page
            var result = new ContentResult
            {
                Content = jsonResponse,
                ContentType = "text/html"
            };
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to connect API {idpUrl}: {ex.Message}", idpUrl, ex.Message);
            throw;
        }
    }
    
    private async Task<IActionResult> WrongAsync()
    {
        // Invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks.
        var rnd = new Random();
        int delay = rnd.Next(2, 6);
        await Task.Delay(TimeSpan.FromSeconds(delay));
        throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
    }
}
