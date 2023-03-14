using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Filters
{
    public class FeatureNotEnabledExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger>();
            var routeData = context.RouteData;
            if(context.Exception is FeatureNotEnabledException ex)
            {
                var action = routeData.Values["action"] ?? "action";
                var controller = routeData.Values["controller"] ?? "controller";
                var route = $"/{controller}/{action}".ToLower();
                logger.LogError("Unable to navigate to route '{r:l}' because required feature '{f:l}' is not enabled", route, ex.FeatureDescription);


                context.HttpContext.Response.Clear();
                context.HttpContext.Response.Redirect("~/home");
            }
        }
    }
}
