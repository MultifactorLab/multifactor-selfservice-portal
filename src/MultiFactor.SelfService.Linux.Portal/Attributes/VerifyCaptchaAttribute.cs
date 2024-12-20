﻿using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Attributes
{
    public class VerifyCaptchaAttribute : ActionFilterAttribute
    {
        private readonly CaptchaRequired _captchaRequired;
        public VerifyCaptchaAttribute(CaptchaRequired captchaRequired = CaptchaRequired.Always)
        {
            _captchaRequired = captchaRequired;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var ctx = context.HttpContext;

            var config = ctx.RequestServices.GetRequiredService<PortalSettings>();
            if (!config.CaptchaSettings.IsCaptchaEnabled(_captchaRequired))           
            {
                await base.OnActionExecutionAsync(context, next);
                return;
            }

            var captchaVerifierResolver = ctx.RequestServices.GetRequiredService<CaptchaVerifierResolver>();
            var captchaVerifier = captchaVerifierResolver();
            var res = await captchaVerifier.VerifyCaptchaAsync(ctx.Request);
            if (res.Success)
            {
                await base.OnActionExecutionAsync(context, next);
                return;
            }

            var logger = ctx.RequestServices.GetService<ILogger<VerifyCaptchaAttribute>>();
            logger?.LogWarning("Captcha verification failed: {msg:l}", res.Message);

            var localizer = ctx.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();
            context.ModelState.AddModelError(string.Empty, localizer.GetString("Captcha.Failed"));

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
