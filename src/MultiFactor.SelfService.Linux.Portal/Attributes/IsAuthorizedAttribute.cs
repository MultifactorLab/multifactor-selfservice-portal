using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;

namespace MultiFactor.SelfService.Linux.Portal.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class IsAuthorizedAttribute : Attribute, IAuthorizationFilter
    {
        private readonly bool _validateUserSession;

        /// <summary>
        /// Creates instance of attribute.
        /// </summary>
        /// <param name="validateUserSession">true: validate cookies, JWT claims and that user is authenticated. false: validate that user is authenticated only.</param>
        public IsAuthorizedAttribute(bool validateUserSession = true)
        {
            _validateUserSession = validateUserSession;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!_validateUserSession) return;
            var tokenVerifier = context.HttpContext.RequestServices.GetRequiredService<TokenVerifier>();
            var cookie = context.HttpContext.Request.Cookies[Constants.COOKIE_NAME];
            if (cookie is null)
            {
                var signOutStory = context.HttpContext.RequestServices.GetRequiredService<SignOutStory>();
                var singleSignOnDto = MultiFactorClaimsDtoBinder.FromRequest(context.HttpContext.Request);
                signOutStory.Execute(singleSignOnDto);
                return;
            }
            var tokenClaims = tokenVerifier.Verify(cookie);
            if(tokenClaims.MustChangePassword)
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
