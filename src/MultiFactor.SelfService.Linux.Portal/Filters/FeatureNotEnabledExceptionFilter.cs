using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Filters
{
    public class FeatureNotEnabledExceptionFilter : ExceptionFilterAttribute
    {
        private ILogger<FeatureNotEnabledExceptionFilter> _logger;
        public FeatureNotEnabledExceptionFilter(ILogger<FeatureNotEnabledExceptionFilter> logger)
        {
            _logger = logger;
        }

        public override void OnException(ExceptionContext context)
        {
            if(context.Exception is FeatureNotEnabledException ex)
            {
                var rd = context.HttpContext.GetRouteData();
                var action = rd.Values["action"] ?? "action";
                var controller = rd.Values["controller"] ?? "controller";
                var route = $"/{controller}/{action}".ToLower();
                _logger.LogWarning("Unable to navigate to route '{r:l}' because required feature '{f:l}' is not enabled.", route, ex.FeatureDescription);

                context.HttpContext.Response.Clear();
                context.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    { "controller", "Account" },
                    { "action", "Logout" }
                });
            };
        }
    }
}
