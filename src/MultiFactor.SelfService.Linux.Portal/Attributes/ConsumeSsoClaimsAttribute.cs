using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders;

namespace MultiFactor.SelfService.Linux.Portal.Attributes
{
    public class ConsumeSsoClaimsAttribute : ActionFilterAttribute
    {
        public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var sso = MultiFactorClaimsDtoBinder.FromRequest(context.HttpContext.Request);
            context.HttpContext.Items[Constants.SsoClaims] = sso;
            return base.OnActionExecutionAsync(context, next);
        }
    }
}
