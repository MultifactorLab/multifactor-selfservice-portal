using System.Linq.Expressions;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class PortalSettingsAccess
    {
        public static TProperty GetPortalSettingsValue<TProperty>(this IConfiguration config, 
            Expression<Func<PortalSettings, TProperty>> propertySelector)
        {
            if (propertySelector is null) throw new ArgumentNullException(nameof(propertySelector));

            var key = GetSettingPath(propertySelector);
            return GetConfigValue<TProperty>(config, $"{PortalSettings.SectionName}:{key}");
        }

        public static TProperty GetConfigValue<TProperty>(this IConfiguration config, string path)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            if (path is null) throw new ArgumentNullException(nameof(path));

            return config.GetValue<TProperty>(path);
        }

        private static string GetSettingPath<T, P>(Expression<Func<T, P>> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            if (action.Body.NodeType != ExpressionType.MemberAccess) throw new Exception("Invalid property name");

            var path = action.ToString().Split('.').Skip(1) ?? Array.Empty<string>();
            return string.Join(":", path);
        }
    }
}
