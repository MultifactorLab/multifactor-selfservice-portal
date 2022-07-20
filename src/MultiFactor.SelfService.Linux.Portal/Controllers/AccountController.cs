using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Models;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    public class AccountController : Controller
    {

        public AccountController()
        {
        }

        public IActionResult Login()
        {
            if (User.Identity != null && !User.Identity.IsAuthenticated)
            {
                // TODO:
                //if (Configuration.AuthenticationMode == AuthenticationMode.Windows && User.Identity != null)
                //{
                //    // Integrated windows authentication.
                //    if (!string.IsNullOrEmpty(User.Identity.Name) && User.Identity.AuthenticationType == "Negotiate")
                //    {
                //        var userName = User.Identity.Name;

                //        _logger.LogInformation("User '{user:l}' authenticated by NTLM/Kerberos", userName);

                //        var samlSessionId = GetMultifactorClaimFromRedirectUrl(userName, MultiFactorClaims.SamlSessionId);
                //        var oidcSessionId = GetMultifactorClaimFromRedirectUrl(userName, MultiFactorClaims.OidcSessionId);

                //        return RedirectToMfa(userName, null, null, null, Request.Url.ToString(), samlSessionId, oidcSessionId);
                //    }
                //}
            }

            return View(new LoginModel());
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
