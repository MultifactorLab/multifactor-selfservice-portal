using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Filters
{
    public class MultiFactorApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<MultiFactorApiExceptionFilter> _logger;

        public MultiFactorApiExceptionFilter(ILogger<MultiFactorApiExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is UnsuccessfulResponseException apiException)
            {
                var rd = context.HttpContext.GetRouteData();
                var action = rd.Values["action"]?.ToString() ?? "action";
                var controller = rd.Values["controller"]?.ToString() ?? "controller";
                
                _logger.LogWarning("MultiFactorApi error in {Controller}/{Action}: {Message:l}", 
                    controller, action, apiException.Message);

                var result = HandleByMessageContent(apiException.Message);

                if (result == null) return;
                
                context.ModelState.AddModelError(string.Empty, apiException.Message);
                context.ExceptionHandled = true;
                context.Result = result;
            }
        }

        private IActionResult HandleByMessageContent(string message)
        {
            return message.ToLowerInvariant() switch
            {
                var msg when msg.Contains("unauthorized") || msg.Contains("access denied") || msg.Contains("usernotregistered") => 
                    new RedirectToActionResult("AccessDenied", "Error", new { }),
                
                _ => new RedirectToActionResult("Index", "Error", new { })
            };
        }
    }
}
