﻿using Microsoft.AspNetCore.Mvc;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    //[OutputCache(NoStore = true, Duration = 0)]
    public abstract class ControllerBase : Controller
    {
        //protected ActionResult SignOut()
        //{
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

        //    return Redirect(returnUrl);
        //}
    }
}
