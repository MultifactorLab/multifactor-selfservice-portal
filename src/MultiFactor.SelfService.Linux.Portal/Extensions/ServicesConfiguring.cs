using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Stories.AddGoogleAuthStory;
using MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeExpiredPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Stories.CheckExpiredPasswordSessionStory;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory;
using MultiFactor.SelfService.Linux.Portal.Stories.GetGoogleAuthKeyStory;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;
using System.Net;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class ServicesConfiguring
    {
        public static WebApplicationBuilder ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            builder.Services
                .AddSession()
                .AddHttpContextAccessor()
                .AddSingleton<SafeHttpContextAccessor>()
                .AddSingleton<TokenVerifier>()
                .AddSingleton<TokenClaimsAccessor>()
                .AddSingleton<DataProtection>()
                .AddSingleton<JsonDataSerializer>()

                .AddTransient<ActiveDirectoryCredentialVerifier>()
                .AddTransient<ActiveDirectoryPasswordChanger>()
                .AddTransient<HttpClientTokenProvider>()
                .AddTransient<HttpMessageInterceptor>()
                .AddTransient<ApplicationHttpClient>()
                .AddTransient<MultiFactorApi>()

                .AddTransient<SignInStory>()
                .AddTransient<SignOutStory>()
                .AddTransient<LoadProfileStory>()
                .AddTransient<AuthenticateSessionStory>()
                .AddTransient<RemoveAuthenticatorStory>()
                .AddTransient<CreateGoogleAuthKeyStory>()
                .AddTransient<AddGoogleAuthStory>()
                .AddTransient<GetApplicationInfoStory>()
                .AddTransient<CheckExpiredPasswordSessionStory>()
                .AddTransient<ChangeExpiredPasswordStory>();

            ConfigureHttpClient(builder);

            return builder;
        }

        private static void ConfigureHttpClient(WebApplicationBuilder builder)
        {
            builder.Services.AddHttpClient<ApplicationHttpClient>((services, client) =>
            {
                var settings = services.GetRequiredService<PortalSettings>();
                client.BaseAddress = new Uri(settings.MultiFactorApiUrl);
            }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();

                var proxySetting = builder.Configuration.GetPortalSettingsValue(x => x.MultiFactorApiProxy);
                if (!string.IsNullOrEmpty(proxySetting))
                {
                    handler.Proxy = BuildProxy(proxySetting);
                }

                return handler;
            }).AddHttpMessageHandler<HttpMessageInterceptor>();
        }

        private static WebProxy BuildProxy(string proxyUri)
        {
            var uri = new Uri(proxyUri);
            var proxy = new WebProxy(uri);
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var credentials = uri.UserInfo.Split(new[] { ':' }, 2);
                proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
            }

            return proxy;
        }
    }
}
