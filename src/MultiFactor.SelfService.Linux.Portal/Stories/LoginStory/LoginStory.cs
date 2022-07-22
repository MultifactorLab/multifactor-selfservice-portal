using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Models;
using MultiFactor.SelfService.Linux.Portal.Services;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoginStory
{
    public record LoginResult(string Error);

    public class LoginStory
    {
        private readonly ActiveDirectoryCredentialVerifier _credentialVerifier;
        private readonly PortalSettings _settings;
        private readonly IStringLocalizer<Login> _localizer;

        public LoginStory(ActiveDirectoryCredentialVerifier credentialVerifier, PortalSettings settings, IStringLocalizer<Login> localizer)
        {
            _credentialVerifier = credentialVerifier;
            _settings = settings;
            _localizer = localizer;
        }

        public async Task ExecuteAsync(LoginModel model)
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

            ////authenticated ok
            //if (adValidationResult.IsAuthenticated)
            //{
            //    var samlSessionId = GetMultifactorClaimFromRedirectUrl(model.UserName, MultiFactorClaims.SamlSessionId);
            //    var oidcSessionId = GetMultifactorClaimFromRedirectUrl(model.UserName, MultiFactorClaims.OidcSessionId);

            //    if (!string.IsNullOrEmpty(samlSessionId) && adValidationResult.IsBypass)
            //    {
            //        return ByPassSamlSession(model.UserName, samlSessionId);
            //    }

            //    return RedirectToMfa(model.UserName, adValidationResult.DisplayName, adValidationResult.Email, adValidationResult.Phone, model.MyUrl, samlSessionId, oidcSessionId);
            //}
            //else
            //{
            //    if (adValidationResult.UserMustChangePassword && Configuration.Current.EnablePasswordManagement)
            //    {
            //        var dataProtectionService = new DataProtectionService();
            //        var encryptedPassword = dataProtectionService.Protect(model.Password.Trim());
            //        Session[Constants.SESSION_EXPIRED_PASSWORD_USER_KEY] = model.UserName.Trim();
            //        Session[Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY] = encryptedPassword;

            //        return RedirectToAction("Change", "ExpiredPassword");
            //    }

            //    ModelState.AddModelError(string.Empty, Resources.AccountLogin.WrongUserNameOrPassword);

            //    // Invalid credentials, freeze response for 2-5 seconds to prevent brute-force attacks
            //    var rnd = new Random();
            //    int delay = rnd.Next(2, 6);
            //    await Task.Delay(TimeSpan.FromSeconds(delay));
            //}

            return Task.CompletedTask;
        }

        //private string GetMultifactorClaimFromRedirectUrl(string login, string claim)
        //{
        //    var redirectUrl = FormsAuthentication.GetRedirectUrl(login, false);
        //    if (!string.IsNullOrEmpty(redirectUrl))
        //    {
        //        var queryIndex = redirectUrl.IndexOf("?");
        //        if (queryIndex >= 0)
        //        {
        //            var query = HttpUtility.ParseQueryString(redirectUrl.Substring(queryIndex));
        //            return query[claim];
        //        }
        //    }
        //}

        //private ActionResult RedirectToMfa(string login, string displayName, string email, 
        //    string phone, string documentUrl, string samlSessionId, 
        //    string oidcSessionId, bool mustResetPassword = false)
        //{
        //    //public url from browser if we behind nginx or other proxy
        //    var currentUri = new Uri(documentUrl);
        //    var noLastSegment = $"{currentUri.Scheme}://{currentUri.Authority}";

        //    for (int i = 0; i < currentUri.Segments.Length - 1; i++)
        //    {
        //        noLastSegment += currentUri.Segments[i];
        //    }

        //    noLastSegment = noLastSegment.Trim("/".ToCharArray()); // remove trailing /

        //    var postbackUrl = noLastSegment + "/PostbackFromMfa";

        //    //exra params
        //    var claims = new Dictionary<string, string>
        //    {
        //        { MultiFactorClaims.RawUserName, login }    //as specifyed by user
        //    };

        //    if (mustResetPassword)
        //    {
        //        claims.Add(MultiFactorClaims.ChangePassword, "true");
        //    }
        //    else
        //    {
        //        if (samlSessionId != null)
        //        {
        //            claims.Add(MultiFactorClaims.SamlSessionId, samlSessionId);
        //        }
        //        if (oidcSessionId != null)
        //        {
        //            claims.Add(MultiFactorClaims.OidcSessionId, oidcSessionId);
        //        }
        //    }


        //    var client = new MultiFactorApiClient();
        //    var accessPage = client.CreateAccessRequest(login, displayName, email, phone, postbackUrl, claims);

        //    return RedirectPermanent(accessPage.Url);
        //}
    }
}
