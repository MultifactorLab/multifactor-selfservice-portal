using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory
{
    public class SignInStory
    {
        private readonly ActiveDirectoryCredentialVerifier _credentialVerifier;
        private readonly DataProtection _dataProtection;
        private readonly MultiFactorApi _api;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer _localizer;
        private readonly ILogger<SignInStory> _logger;

        public SignInStory(ActiveDirectoryCredentialVerifier credentialVerifier,
            DataProtection dataProtection,
            MultiFactorApi api,
            SafeHttpContextAccessor contextAccessor,
            PortalSettings settings,
            IStringLocalizer<SharedResource> localizer,
            ILogger<SignInStory> logger)
        {
            _credentialVerifier = credentialVerifier ?? throw new ArgumentNullException(nameof(credentialVerifier));
            _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> ExecuteAsync(LoginViewModel model, SingleSignOnDto sso)
        {
            if (_settings.RequiresUserPrincipalName)
            {
                // AD requires UPN check
                var userName = LdapIdentity.ParseUser(model.UserName);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
                }
            }

            // AD credential check
            var adValidationResult = await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());
            if (adValidationResult.IsAuthenticated)
            {
                if (sso.HasSamlSession() && adValidationResult.IsBypass)
                {
                    return new RedirectToActionResult("ByPassSamlSession", "account", new { username = model.UserName, samlSession = sso.SamlSessionId });
                }

                return await RedirectToMfa(model.UserName, adValidationResult, model.MyUrl, sso);
            }

            if (adValidationResult.UserMustChangePassword && _settings.EnablePasswordManagement)
            {
                var encryptedPassword = _dataProtection.Protect(model.Password.Trim());

                _contextAccessor.HttpContext.Session.SetString(Constants.SESSION_EXPIRED_PASSWORD_USER_KEY, model.UserName.Trim());
                _contextAccessor.HttpContext.Session.SetString(Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY, encryptedPassword);

                return new RedirectToActionResult("Change", "ExpiredPassword", new { });
            }

            // Invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));

            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }

        private async Task<IActionResult> RedirectToMfa(string username,
            CredentialVerificationResult verificationResult,
            string documentUrl,
            SingleSignOnDto sso,
            bool mustResetPassword = false)
        {
            // public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(documentUrl);
            var noLastSegment = string.Format("{0}://{1}", currentUri.Scheme, currentUri.Authority);

            for (int i = 0; i < currentUri.Segments.Length - 1; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            // remove trailing /
            noLastSegment = noLastSegment.Trim("/".ToCharArray());

            var postbackUrl = noLastSegment + "/PostbackFromMfa";

            // exra params
            var claims = GetClaims(username, mustResetPassword, sso);

            var accessPage = await _api.CreateAccessRequestAsync(username,
                verificationResult.DisplayName,
                verificationResult.Email,
                verificationResult.Phone,
                postbackUrl,
                claims);

            return new RedirectResult(accessPage.Url, true);
        }

        private static IReadOnlyDictionary<string, string> GetClaims(string username, bool mustResetPassword, SingleSignOnDto sso)
        {
            var claims = new Dictionary<string, string>
            {
                // as specifyed by user
                { Constants.MultiFactorClaims.RawUserName, username }
            };

            if (mustResetPassword)
            {
                claims.Add(Constants.MultiFactorClaims.ChangePassword, "true");
                return claims;
            }

            if (sso.HasSamlSession())
            {
                claims.Add(Constants.MultiFactorClaims.SamlSessionId, sso.SamlSessionId);
            }

            if (sso.HasOidcSession())
            {
                claims.Add(Constants.MultiFactorClaims.OidcSessionId, sso.OidcSessionId);
            }

            return claims;
        }
    }
}
