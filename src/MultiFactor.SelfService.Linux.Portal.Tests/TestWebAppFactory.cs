using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public class TestWebAppFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(ConfigureServices);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            OnHostCreated(host.Services);
            return host;
        }

        protected virtual void ConfigureServices(IServiceCollection services) { }
        protected virtual void OnHostCreated(IServiceProvider provider) { } 
    }
}
