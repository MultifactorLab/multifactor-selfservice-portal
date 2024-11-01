using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Linq.Expressions;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class PortalSettingsAccess
    {
        public static TProperty GetPortalSettingsValue<TProperty>(this IConfiguration config, 
            Expression<Func<PortalSettings, TProperty>> propertySelector)
        {
            ArgumentNullException.ThrowIfNull(propertySelector);

            var key = ClassPropertyAccessor.GetPropertyPath(propertySelector, ":");
            return GetConfigValue<TProperty>(config, $"{PortalSettings.SectionName}:{key}");
        }

        public static TProperty GetConfigValue<TProperty>(this IConfiguration config, string path)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(path);

            return config.GetValue<TProperty>(path);
        }
    }
}
