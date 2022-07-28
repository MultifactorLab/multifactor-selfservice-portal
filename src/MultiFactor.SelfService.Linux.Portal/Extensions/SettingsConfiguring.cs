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

            return applicationBuilder;
        }
    }
}
