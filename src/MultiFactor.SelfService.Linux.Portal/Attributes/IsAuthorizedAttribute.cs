using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Stories.Authenticate;

namespace MultiFactor.SelfService.Linux.Portal.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class IsAuthorizedAttribute : Attribute, IAuthorizationFilter
    {
        private readonly bool _skipPasswordVerification = false;
        /// <summary>
        /// Creates instance of attribute.
        /// </summary>
        /// <param name="skipPasswordVerification">true: validate cookies, JWT claims and that user is authenticated. false: validate that user is authenticated only.</param>
        public IsAuthorizedAttribute(bool skipPasswordVerification = false)
        {
            _skipPasswordVerification = skipPasswordVerification;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var tokenVerifier = context.HttpContext.RequestServices.GetRequiredService<TokenVerifier>();
            var cookie = context.HttpContext.Request.Cookies[Constants.COOKIE_NAME];
            if (cookie is null)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "action", "Auth" },
                    { "controller", "Account" }
                });
                
                return;
            }
            var tokenClaims = tokenVerifier.Verify(cookie);
            if(tokenClaims.MustChangePassword && !_skipPasswordVerification)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "action", "Change" },
                    { "controller", "ExpiredPassword" }
                });
            }
        }
    }
}
