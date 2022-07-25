using MultiFactor.SelfService.Linux.Portal.Core;
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
                .AddTransient<MultiFactorSelfServiceApiClient>()
                .AddTransient<ActiveDirectoryCredentialVerifier>()
                .AddTransient<SignOutStory>()
                .AddTransient<LoginStory>()
                .AddTransient<LoadProfileStory>()
                .AddTransient<SafeHttpContextAccessor>();

            builder.Services.AddHttpClient<ApplicationHttpClient>(o =>
            {
                o.BaseAddress = new Uri(builder.Configuration.GetPortalSettingsValue(x => x.MultiFactorApiUrl));
            });
        }
    }
}
