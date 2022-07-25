using Microsoft.Extensions.DependencyInjection;
using MultiFactor.SelfService.Linux.Portal.Services.Api;

namespace MultiFactor.SelfService.Linux.Portal.ServicesConfiguring
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<MultiFactorSelfServiceApiClient>();
            services.AddScoped<ActiveDirectoryCredentialVerifier>();
            services.AddScoped<DataProtection>();
            services.AddScoped<SignOutStory>();
            services.AddScoped<LoginStory>();
        }
    }
}