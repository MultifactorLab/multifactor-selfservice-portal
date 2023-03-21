using MultiFactor.SelfService.Linux.Portal.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Core.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _requestDelegate;
        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _requestDelegate = next;
        }

        public async Task InvokeAsync(HttpContext context, ILogger<ExceptionHandlingMiddleware> logger)
        {
            try
            {
                await _requestDelegate.Invoke(context);
            } 
            catch (Exception ex)
            {
                if (ex is FeatureNotEnabledException featureEx)
                {
                    var rd = context.GetRouteData();
                    var action = rd.Values["action"] ?? "action";
                    var controller = rd.Values["controller"] ?? "controller";
                    var route = $"/{controller}/{action}".ToLower();
                    logger.LogWarning("Unable to navigate to route '{r:l}' because required feature '{f:l}' is not enabled.", route, featureEx.FeatureDescription);

                    context.Response.Clear();
                    context.Response.Redirect("/Account/Logout");
                    return;
                }
                throw;
            }
        }
    }
}
