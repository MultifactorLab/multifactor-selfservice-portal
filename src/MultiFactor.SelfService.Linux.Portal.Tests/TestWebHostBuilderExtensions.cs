using Microsoft.AspNetCore.Hosting;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Linq.Expressions;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public class SettingsOverriding
    {
        private readonly IWebHostBuilder _hostBuilder;

        public SettingsOverriding(IWebHostBuilder hostBuilder)
        {
            _hostBuilder = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));
        }

        public SettingsOverriding UseSettings<TProperty>(Expression<Func<PortalSettings, TProperty>> property, 
            TProperty value)
        {
            var key = ClassPropertyAccessor.GetPropertyPath(property);
            _hostBuilder.UseSetting($"{nameof(PortalSettings)}:{key}", value?.ToString());
            return this;
        }
    }
}
