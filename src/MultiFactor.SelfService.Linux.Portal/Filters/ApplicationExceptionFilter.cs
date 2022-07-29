using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Filters
{
    public class ApplicationExceptionFilter : IExceptionFilter, IActionFilter
    {
        private IDictionary<string, object?> _actionAttributes = new Dictionary<string, object?>();

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _actionAttributes = context.ActionArguments;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is UnauthorizedException ue)
            {
                context.ModelState.AddModelError(string.Empty, context.Exception.Message);
                context.ExceptionHandled = true;
                context.Result = new RedirectToActionResult("logout", "account", new { });
            }

            if (context.Exception is ModelStateErrorException me)
            {
                context.ModelState.AddModelError(string.Empty, context.Exception.Message);
                context.ExceptionHandled = true;
                context.Result = new ViewResult();
            }
        }
    }
}
