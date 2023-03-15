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
            logger.LogError("In middleware");
            await _requestDelegate.Invoke(context);
        }
    }
}
