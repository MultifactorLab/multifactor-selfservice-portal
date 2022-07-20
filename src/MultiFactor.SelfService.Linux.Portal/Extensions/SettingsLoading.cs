using Serilog;
using System.Linq.Expressions;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class SettingsLoading
    {
        public static void AddSettings(this WebApplicationBuilder applicationBuilder, string[] args)
        {
            applicationBuilder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true)
                      .AddXmlFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.xml", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();

                if (args.Any())
                {
                    config.AddCommandLine(args);
                }
            });
        }

        public static void LoadPortalSettings(this WebApplicationBuilder applicationBuilder)
        {
            try
            {
                var settings = GetSettings(applicationBuilder) ?? throw new Exception("Can't find PortalSettings section in appsettings");
                ValidateSettings(settings);
                applicationBuilder.Services.AddSingleton(settings);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Unable to start");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static TProperty? GetPortalSettingsValue<TProperty>(this IConfiguration config, Expression<Func<PortalSettings, TProperty>> propertySelector,
            TProperty? defaultValue = default)
        {
            if (propertySelector is null) throw new ArgumentNullException(nameof(propertySelector));

            var key = GetSettingPath(propertySelector);
            return GetConfigValue(config, $"{PortalSettings.SectionName}:{key}", defaultValue);
        }

        public static TProperty? GetConfigValue<TProperty>(this IConfiguration config, string path, 
            TProperty? defaultValue = default)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));
            if (path is null) throw new ArgumentNullException(nameof(path));
            
            return config.GetValue(path, defaultValue);
        }

        private static string GetSettingPath<T, P>(Expression<Func<T, P>> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            if (action.Body.NodeType != ExpressionType.MemberAccess) throw new Exception("Invalid property name");

            var path = action.ToString().Split('.').Skip(1) ?? Array.Empty<string>();
            return string.Join(":", path);
        }

        private static PortalSettings GetSettings(WebApplicationBuilder applicationBuilder)
        {
            var section = applicationBuilder.Configuration.GetSection(nameof(PortalSettings));
            return section.Get<PortalSettings>(o =>
            {
                o.BindNonPublicProperties = true;
            });
        }

        private static void ValidateSettings(PortalSettings settings)
        {
            if (string.IsNullOrEmpty(settings.CompanyName)) ThrowConfigError(nameof(PortalSettings.CompanyName));
            if (string.IsNullOrEmpty(settings.CompanyDomain)) ThrowConfigError(nameof(PortalSettings.CompanyDomain));
            if (string.IsNullOrEmpty(settings.CompanyLogoUrl)) ThrowConfigError(nameof(PortalSettings.CompanyLogoUrl));
            if (string.IsNullOrEmpty(settings.MultiFactorApiUrl)) ThrowConfigError(nameof(PortalSettings.MultiFactorApiUrl));
            if (string.IsNullOrEmpty(settings.MultiFactorApiKey)) ThrowConfigError(nameof(PortalSettings.MultiFactorApiKey));
            if (string.IsNullOrEmpty(settings.MultiFactorApiSecret)) ThrowConfigError(nameof(PortalSettings.MultiFactorApiSecret));
        }

        private static void ThrowConfigError(string propertyName)
        {
            throw new Exception($"Configuration error: '{propertyName}' element not found");
        }
    }
}
