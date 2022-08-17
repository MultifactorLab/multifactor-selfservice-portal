using MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Integrations.Google;
using MultiFactor.SelfService.Linux.Portal.Integrations.Google.ReCaptcha;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.AddYandexAuthStory;
using MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeActiveSyncDeviceStateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeExpiredPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeValidPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Stories.CheckExpiredPasswordSessionStory;
using MultiFactor.SelfService.Linux.Portal.Stories.CreateYandexAuthKeyStory;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.Stories.RemoveAuthenticator;
using MultiFactor.SelfService.Linux.Portal.Stories.SearchExchangeActiveSyncDevicesStory;
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
                .AddSingleton<JsonPayloadLogger>()
                .AddSingleton<DeviceAccessStateNameLocalizer>()
                .AddSingleton<ActiveDirectoryCredentialVerifier>()
                .AddSingleton<ActiveDirectoryPasswordChanger>()
                .AddSingleton<HttpClientTokenProvider>()
                .AddSingleton<ExchangeActiveSyncDevicesSearcher>()
                .AddSingleton<ExchangeActiveSyncDeviceStateChanger>()

                .AddTransient<HttpMessageInterceptor>()
                .AddTransient<ICaptchaVerifier, GoogleReCaptchaVerifier>()

                .AddTransient<SignInStory>()
                .AddTransient<SignOutStory>()
                .AddTransient<LoadProfileStory>()
                .AddTransient<AuthenticateSessionStory>()
                .AddTransient<RemoveAuthenticatorStory>()
                .AddTransient<CreateYandexAuthKeyStory>()
                .AddTransient<AddYandexAuthStory>()
                .AddTransient<GetApplicationInfoStory>()
                .AddTransient<CheckExpiredPasswordSessionStory>()
                .AddTransient<ChangeExpiredPasswordStory>()
                .AddTransient<ChangeValidPasswordStory>()
                .AddTransient<SearchExchangeActiveSyncDevicesStory>()
                .AddTransient<ChangeActiveSyncDeviceStateStory>();

            ConfigureMultifactorApi(builder);
            ConfigureGoogleApi(builder);

            return builder;
        }

        private static void ConfigureMultifactorApi(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<MultiFactorApi>()
                .AddTransient<MultifactorHttpClientAdapterFactory>()
                .AddHttpClient<MultifactorHttpClientAdapterFactory>((services, client) =>
                {
                    var settings = services.GetRequiredService<PortalSettings>();
                    client.BaseAddress = new Uri(settings.MultiFactorApiSettings.ApiUrl);
                }).ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new HttpClientHandler();

                    var proxySetting = builder.Configuration.GetPortalSettingsValue(x => x.MultiFactorApiSettings.ApiProxy);
                    if (!string.IsNullOrEmpty(proxySetting))
                    {
                        handler.Proxy = BuildProxy(proxySetting);
                    }

                    return handler;
                }).AddHttpMessageHandler<HttpMessageInterceptor>();
        }

        private static void ConfigureGoogleApi(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<GoogleReCaptcha2Api>()
                .AddTransient<GoogleHttpClientAdapterFactory>()
                .AddHttpClient<GoogleHttpClientAdapterFactory>((services, client) =>
                {
                    client.BaseAddress = new Uri("https://www.google.com/recaptcha/api/");
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
