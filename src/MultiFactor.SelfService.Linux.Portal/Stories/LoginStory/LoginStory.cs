using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Models;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoginStory
{
    public class LoginStory
    {
        private readonly ActiveDirectoryCredentialVerifier _credentialVerifier;
        private readonly DataProtection _dataProtection;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer<Login> _localizer;

        public LoginStory(ActiveDirectoryCredentialVerifier credentialVerifier, DataProtection dataProtection, PortalSettings settings, IStringLocalizer<Login> localizer)
        {
            _credentialVerifier = credentialVerifier;
            _dataProtection = dataProtection;
            _settings = settings;
            _localizer = localizer;
        }

        public async Task<IActionResult> ExecuteAsync(LoginModel model, MultiFactorClaimsDto claims, HttpContext context)
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
                if (!string.IsNullOrEmpty(claims.SamlSession) && adValidationResult.IsBypass)
                {
                    //return ByPassSamlSession(model.UserName, claims.SamlSession);
                }

                return RedirectToMfa(model.UserName, 
                    adValidationResult, 
                    model.MyUrl, 
                    claims);
            }

            if (adValidationResult.UserMustChangePassword && _settings.EnablePasswordManagement)
            {
                var encryptedPassword = _dataProtection.Protect(model.Password.Trim());
                context.Session.SetString(Constants.SESSION_EXPIRED_PASSWORD_USER_KEY, model.UserName.Trim());
                context.Session.SetString(Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY, encryptedPassword);

                return new RedirectToActionResult("Change", "ExpiredPassword", new { });
            }

            // Invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks
            var rnd = new Random();
            int delay = rnd.Next(2, 6);
            await Task.Delay(TimeSpan.FromSeconds(delay));

            throw new ModelStateErrorException(_localizer.GetString("WrongUserNameOrPassword"));
        }

        private IActionResult RedirectToMfa(string username, CredentialVerificationResult verificationResult, string documentUrl, 
            MultiFactorClaimsDto mfClaims, 
            bool mustResetPassword = false)
        {
            //public url from browser if we behind nginx or other proxy
            var currentUri = new Uri(documentUrl);
            var noLastSegment = string.Format("{0}://{1}", currentUri.Scheme, currentUri.Authority);

            for (int i = 0; i < currentUri.Segments.Length - 1; i++)
            {
                noLastSegment += currentUri.Segments[i];
            }

            noLastSegment = noLastSegment.Trim("/".ToCharArray()); // remove trailing /

            var postbackUrl = noLastSegment + "/PostbackFromMfa";

            //exra params
            var claims = new Dictionary<string, string>
            {
                { MultiFactorClaims.RawUserName, username }    //as specifyed by user
            };

            if (mustResetPassword)
            {
                claims.Add(MultiFactorClaims.ChangePassword, "true");
            }
            else
            {
                if (mfClaims.HasSamlSession != null)
                {
                    claims.Add(MultiFactorClaims.SamlSessionId, mfClaims.SamlSession);
                }
                if (mfClaims.HasOidcSession != null)
                {
                    claims.Add(MultiFactorClaims.OidcSessionId, mfClaims.OidcSession);
                }
            }

            throw new NotImplementedException();


            //var client = new MultiFactorApiClient();
            //var accessPage = client.CreateAccessRequest(username, verificationResult.DisplayName, verificationResult.Email, verificationResult.Phone, postbackUrl, claims);

            //return RedirectPermanent(accessPage.Url);
        }
    }
}
