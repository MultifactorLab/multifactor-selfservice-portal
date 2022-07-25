using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.Stories.LoginStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class ServicesConfiguring
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<DataProtection>()
                .AddScoped<MultiFactorSelfServiceApiClient>()
                .AddScoped<ActiveDirectoryCredentialVerifier>()
                .AddScoped<SignOutStory>()
                .AddScoped<LoginStory>()
                .AddScoped<LoadProfileStory>();

            builder.Services.AddHttpClient<ApplicationHttpClient>(o =>
            {
                o.BaseAddress = new Uri(builder.Configuration.GetPortalSettingsValue(x => x.MultiFactorApiUrl));
            });
        }
    }
}
