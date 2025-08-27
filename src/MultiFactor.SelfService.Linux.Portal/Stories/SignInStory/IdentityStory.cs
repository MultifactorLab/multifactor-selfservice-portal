using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory
{
    public class IdentityStory
    {
        private readonly CredentialVerifier _credentialVerifier;
        private readonly IMultiFactorApi _api;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer _localizer;
        private readonly ClaimsProvider _claimsProvider;
        private readonly ILogger<SignInStory> _logger;

        public IdentityStory(CredentialVerifier credentialVerifier,
            IMultiFactorApi api,
            SafeHttpContextAccessor contextAccessor,
            PortalSettings settings,
            IStringLocalizer<SharedResource> localizer,
            ILogger<SignInStory> logger,
            ClaimsProvider claimsProvider)
        {
            _credentialVerifier = credentialVerifier ?? throw new ArgumentNullException(nameof(credentialVerifier));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claimsProvider = claimsProvider ?? throw new ArgumentNullException(nameof(claimsProvider));
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

            // 2fa before authn
            var identity = model.UserName;
            var sso = _contextAccessor.SafeGetSsoClaims();
            // in common case
            if (!_settings.NeedPrebindInfo())
            {
                var credResult = CredentialVerificationResult.BeforeAuthn(model.UserName);
                _contextAccessor.HttpContext.Items[Constants.CredentialVerificationResult] = credResult;
                return await RedirectToMfa(credResult, model.MyUrl);
            }

            var adResult = await _credentialVerifier.VerifyMembership(model.UserName.Trim());
            _contextAccessor.HttpContext.Items[Constants.CredentialVerificationResult] = adResult;
            if (_settings.ActiveDirectorySettings.UseUpnAsIdentity)
            {
                identity = adResult.UserPrincipalName;
            }

            // sso session can skip 2fa, so go to pass entered
            if (adResult.IsBypass && sso.HasSamlSession())
            {
                var res = new ViewResult
                {
                    ViewName = "Authn",
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        // create new model in order to pass new identity - it may be upn instead samAccountName
                        Model = new IdentityViewModel
                        {
                            UserName = identity,
                            Password = model.Password,
                            MyUrl = model.MyUrl,
                            AccessToken = model.AccessToken
                        }
                    }
                };
                _logger.LogInformation("Bypass second factor for user '{user:l}'", identity);
                return res;
                //return new RedirectToActionResult("ByPassSamlSession", "Account", new { username = model.UserName, samlSession = sso.SamlSessionId });
            }

            return await RedirectToMfa(adResult, model.MyUrl);
        }

        private async Task<IActionResult> WrongAsync()
        {
            // Invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks.
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));
            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }

        private async Task<IActionResult> RedirectToMfa(CredentialVerificationResult verificationResult, string documentUrl)
        {
            var postbackUrl = documentUrl.BuildPostbackUrl();
            var claims = _claimsProvider.GetClaims();
            var username = GetIdentity(verificationResult);

            var personalData = new PersonalData(
                verificationResult.DisplayName,
                verificationResult.Email,
                verificationResult.Phone,
                _settings.MultiFactorApiSettings.PrivacyModeDescriptor);

            var accessPage = await _api.CreateAccessRequestAsync(username,
                personalData.Name,
                personalData.Email,
                personalData.Phone,
                postbackUrl,
                claims);

            return new RedirectResult(accessPage.Url, true);
        }

        private string GetIdentity(CredentialVerificationResult verificationResult)
        {
            return !string.IsNullOrWhiteSpace(verificationResult.CustomIdentity)
                ? verificationResult.CustomIdentity
                : verificationResult.Username;
        }

        public async Task<IActionResult> ByPassSamlSession(string username, string samlSession)
        {
            var page = await _api.CreateSamlBypassRequestAsync(username, samlSession);
            var result = new ViewResult
            {
                ViewName = "ByPassSamlSession",
                ViewData =
                {
                    Model = page
                }
            };
            return result;
        }
    }
}