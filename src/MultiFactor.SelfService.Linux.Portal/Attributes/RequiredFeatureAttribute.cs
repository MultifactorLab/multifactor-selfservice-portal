using Microsoft.AspNetCore.Mvc.Filters;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.ComponentModel;

namespace MultiFactor.SelfService.Linux.Portal.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RequiredFeatureAttribute : ActionFilterAttribute
    {
        private readonly ApplicationFeature _requiredFeatureFlags;

        public RequiredFeatureAttribute(ApplicationFeature requiredFeatureFlags)
        {
            _requiredFeatureFlags = requiredFeatureFlags;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var configuration = httpContext.RequestServices.GetRequiredService<PortalSettings>();
            if (_requiredFeatureFlags.HasFlag(ApplicationFeature.PasswordManagement) && !configuration.EnablePasswordManagement)
            {
                throw new FeatureNotEnabledException(ApplicationFeature.PasswordManagement.GetEnumDescription());
            }

            if (_requiredFeatureFlags.HasFlag(ApplicationFeature.ExchangeActiveSyncDevicesManagement) && !configuration.EnableExchangeActiveSyncDevicesManagement)
            {
                throw new FeatureNotEnabledException(ApplicationFeature.ExchangeActiveSyncDevicesManagement.GetEnumDescription());
            }
        }
    }

    [Flags]
    public enum ApplicationFeature
    {
        None = 0,

        [Description("Password Management")]
        PasswordManagement = 1 << 0,
        
        [Description("Exchange Acitve Sync Device Management")]
        ExchangeActiveSyncDevicesManagement = 1 << 1
    }
}
