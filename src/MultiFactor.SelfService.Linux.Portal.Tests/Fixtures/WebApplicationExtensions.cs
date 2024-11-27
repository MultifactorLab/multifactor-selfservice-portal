using Microsoft.AspNetCore.Builder;
using MultiFactor.SelfService.Linux.Portal.Core.Configuration.XmlConfig;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

public static class WebApplicationExtensions
{
    public static WebApplicationBuilder AddConfiguration(
        this WebApplicationBuilder applicationBuilder,
        string configPath,
        bool optional = false,
        bool reloadOnChange = false)
    {
        applicationBuilder.Configuration.AddXmlFile(configPath, optional, reloadOnChange);
        return applicationBuilder;
    }
}