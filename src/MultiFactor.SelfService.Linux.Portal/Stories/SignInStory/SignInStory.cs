using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Models;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory
{
    public class SignInStory
    {
        private readonly ActiveDirectoryCredentialVerifier _credentialVerifier;
        private readonly DataProtection _dataProtection;
        private readonly MultiFactorApi _api;
        private readonly SafeHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer<Login> _localizer;

        public SignInStory(ActiveDirectoryCredentialVerifier credentialVerifier,
            DataProtection dataProtection,
            MultiFactorApi api,
            SafeHttpContextAccessor contextAccessor,
            PortalSettings settings,
            IStringLocalizer<Login> localizer)
        {
            _credentialVerifier = credentialVerifier ?? throw new ArgumentNullException(nameof(credentialVerifier));
            _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public async Task<IActionResult> ExecuteAsync(LoginModel model, MultiFactorClaimsDto claims)
        {
            if (_settings.RequiresUserPrincipalName)
            {
                // AD requires UPN check
                var userName = LdapIdentity.ParseUser(model.UserName);
                if (userName.Type != IdentityType.UserPrincipalName)
                {
                    // TODO
                    throw new ModelStateErrorException(_localizer.GetString("UserNameUpnRequired"));
                }
            }

            // AD credential check
            var adValidationResult = await _credentialVerifier.VerifyCredentialAsync(model.UserName.Trim(), model.Password.Trim());
            if (adValidationResult.IsAuthenticated)
            {
                if (claims.HasSamlSession() && adValidationResult.IsBypass)
                {
                    //return ByPassSamlSession(model.UserName, claims.SamlSession);
                }

                return await RedirectToMfa(model.UserName, adValidationResult, model.MyUrl, claims);
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
            MultiFactorClaimsDto mfClaims,
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
            var claims = GetClaims(username, mustResetPassword, mfClaims);

            var accessPage = await _api.CreateAccessRequestAsync(username,
                verificationResult.DisplayName,
                verificationResult.Email,
                verificationResult.Phone,
                postbackUrl,
                claims);

            return new RedirectResult(accessPage.Url, true);
        }

        private static IReadOnlyDictionary<string, string> GetClaims(string username, bool mustResetPassword, MultiFactorClaimsDto mfClaims)
        {
            var claims = new Dictionary<string, string>
            {
                // as specifyed by user
                { MultiFactorClaims.RawUserName, username }
            };

            if (mustResetPassword)
            {
                claims.Add(MultiFactorClaims.ChangePassword, "true");
                return claims;
            }

            if (mfClaims.HasSamlSession())
            {
                claims.Add(MultiFactorClaims.SamlSessionId, mfClaims.SamlSession);
            }

            if (mfClaims.HasOidcSession())
            {
                claims.Add(MultiFactorClaims.OidcSessionId, mfClaims.OidcSession);
            }

            return claims;
        }
    }
}
