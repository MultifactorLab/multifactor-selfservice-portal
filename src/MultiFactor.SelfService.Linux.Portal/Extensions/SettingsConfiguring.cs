using XmlConfigurationExtensions = MultiFactor.SelfService.Linux.Portal.Core.Configuration.XmlConfig.XmlConfigurationExtensions;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class SettingsConfiguring
    {
        public static WebApplicationBuilder AddConfiguration(this WebApplicationBuilder applicationBuilder, string[] args)
        {
            applicationBuilder.Host.ConfigureAppConfiguration((hostingContext, configBuilder) =>
            {
                configBuilder.AddXmlFile("appsettings.xml", optional: true, reloadOnChange: true)
                     .AddXmlFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.xml", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

                if (args.Any())
                {
                    configBuilder.AddCommandLine(args);
                }
            });

            if (applicationBuilder.Environment.EnvironmentName == "localhost")
            {
                applicationBuilder.Configuration.AddUserSecrets<Program>();
            }

            return applicationBuilder;
        }
    }
}
