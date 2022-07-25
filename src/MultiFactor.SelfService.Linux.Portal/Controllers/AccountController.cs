using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Models;
using MultiFactor.SelfService.Linux.Portal.Services.Api;
using MultiFactor.SelfService.Linux.Portal.Stories.LoginStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    public class ModelStateErrorExceptionFilterAttribute : IExceptionFilter, IActionFilter
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
            if (context.Exception is not ModelStateErrorException) return;

            //context.ModelState.AddModelError(string.Empty, context.Exception.Message);
            //context.ExceptionHandled = true;
            //context.Result = new ViewResult
            //{

            //};
        }
    }

    public class AccountController : ControllerBase
    {
        public AccountController(SignOutStory signOutStory) : base(signOutStory)
        {
        }

        public IActionResult Login(string returnUrl)
        {
            return View(new LoginModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, [FromServices] LoginStory loginStory)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await loginStory.ExecuteAsync(model, null, HttpContext);
                return result;
            }
            catch (ModelStateErrorException ex)
            {
                // TODO: move this handling logic to the some custom FILTER and register GLOBALLY.
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }   
    }
}
