using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Exceptions;

namespace MultiFactor.SelfService.Linux.Portal.Filters
{
    public class ApplicationExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is UnauthorizedException ue)
            {
                context.ModelState.AddModelError(string.Empty, context.Exception.Message);
                context.ExceptionHandled = true;
                context.Result = new RedirectToActionResult("logout", "account", new { });
            }

            if (context.Exception is AntiforgeryValidationException ave)
            {
                context.ModelState.AddModelError(string.Empty, context.Exception.Message);
                context.ExceptionHandled = true;
                context.Result = new RedirectToActionResult("SessionExpired", "Error", new { });
            }
        }
    }
}
