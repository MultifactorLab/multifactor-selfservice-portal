using XmlConfigurationExtensions = MultiFactor.SelfService.Linux.Portal.Core.Configuration.XmlConfig.XmlConfigurationExtensions;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class SettingsConfiguring
    {
        public static WebApplicationBuilder AddConfiguration(this WebApplicationBuilder applicationBuilder, string[] args)
        {
            applicationBuilder.Host.ConfigureAppConfiguration((hostingContext, configBuilder) =>
            {
                XmlConfigurationExtensions.AddXmlFile(configBuilder, "appsettings.xml", 
                    optional: true, 
                    reloadOnChange: true);

                XmlConfigurationExtensions.AddXmlFile(configBuilder, $"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.xml", 
                    optional: true, 
                    reloadOnChange: true);

                configBuilder.AddEnvironmentVariables();

                if (args.Any())
                {
                    configBuilder.AddCommandLine(args);
                }
            });

            return applicationBuilder;
        }
    }
}
