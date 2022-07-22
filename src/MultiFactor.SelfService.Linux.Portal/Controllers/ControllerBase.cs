using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Dto;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    [ResponseCache(NoStore = true, Duration = 0)]
    public abstract class ControllerBase : Controller
    {
        protected IActionResult SignOut(MultiFactorClaimsDto claims)
        {
        //    FormsAuthentication.SignOut();

        //    //remove mfa cookie
        //    if (Request.Cookies[Constants.COOKIE_NAME] != null)
        //    {
        //        Response.Cookies[Constants.COOKIE_NAME].Expires = DateTime.Now.AddDays(-1);
        //    }

        //    var returnUrl = FormsAuthentication.LoginUrl;
        //    var samlSessionId = Request.QueryString[MultiFactorClaims.SamlSessionId];
        //    if (samlSessionId != null)
        //    {
        //        returnUrl += $"?{MultiFactorClaims.SamlSessionId}={samlSessionId}";
        //    }

        //    var oidcSessionId = Request.QueryString[MultiFactorClaims.OidcSessionId];
        //    if (oidcSessionId != null)
        //    {
        //        returnUrl += $"?{MultiFactorClaims.OidcSessionId}={oidcSessionId}";
        //    }

            return Redirect("returnUrl");
        }
    }
}
