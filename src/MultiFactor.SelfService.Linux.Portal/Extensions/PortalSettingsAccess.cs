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
            if (propertySelector is null) throw new ArgumentNullException(nameof(propertySelector));

            var key = ClassPropertyAccessor.GetPropertyPath<PortalSettings, TProperty>(propertySelector, ":");
            return GetConfigValue<TProperty>(config, $"{PortalSettings.SectionName}:{key}");
        }

        public static TProperty GetConfigValue<TProperty>(this IConfiguration config, string path)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            if (path is null) throw new ArgumentNullException(nameof(path));

            return config.GetValue<TProperty>(path);
        }
    }
}
