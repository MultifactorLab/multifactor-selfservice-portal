using MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
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
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Google;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Google.ReCaptcha;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
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
                .AddPasswordChangingSessionCache()
                .AddSingleton<SafeHttpContextAccessor>()
                .AddSingleton<TokenVerifier>()
                .AddSingleton<TokenClaimsAccessor>()
                .AddSingleton<DataProtection>()
                .AddSingleton<JsonDataSerializer>()
                .AddSingleton<JsonPayloadLogger>()
                .AddSingleton<DeviceAccessStateNameLocalizer>()
                .AddSingleton<CredentialVerifier>()
                .AddSingleton<PasswordChanger>()
                .AddSingleton<HttpClientTokenProvider>()
                .AddSingleton<ExchangeActiveSyncDevicesSearcher>()
                .AddSingleton<ExchangeActiveSyncDeviceStateChanger>()

                .AddSingleton<LdapServerInfoFactory>()
                .AddSingleton(services =>
                {
                    var infoTask = services.GetRequiredService<LdapServerInfoFactory>().CreateServerInfoAsync();
                    return infoTask.Result;
                })

                .AddSingleton<LdapConnectionAdapterFactory>()

                .AddSingleton<LdapBindDnFormatterFactory>()
                .AddSingleton(services => services.GetRequiredService<LdapBindDnFormatterFactory>().CreateFormatter())

                .AddSingleton<PasswordAttributeChangerFactory>()
                .AddSingleton(services => services.GetRequiredService<PasswordAttributeChangerFactory>().CreateChanger())

                .AddSingleton<LdapProfileFilterProvider>()
                .AddSingleton<LdapProfileLoader>()

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
            ConfigureYandexCaptchaApi(builder);
            ConfigureCaptchaVerifier(builder);

            builder.Services.AddHostedService<ApplicationChecker>();
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

        private static void ConfigureCaptchaVerifier(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<GoogleReCaptchaVerifier>();
            builder.Services.AddTransient<YandexCaptchaVerifier>();
            
            builder.Services.AddTransient<CaptchaVerifierResolver>(services => () =>
            {
                var settings = services.GetRequiredService<PortalSettings>();
                if (settings.CaptchaSettings.IsCaptchaEnabled(CaptchaType.Yandex))
                {
                    return services.GetRequiredService<YandexCaptchaVerifier>();
                }

                return services.GetRequiredService<GoogleReCaptchaVerifier>();
            });
        }

        private static void ConfigureYandexCaptchaApi(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<YandexCaptchaApi>()
                .AddTransient<YandexHttpClientAdapterFactory>()
                .AddHttpClient<YandexHttpClientAdapterFactory>((services, client) =>
                {
                    client.BaseAddress = new Uri("https://captcha-api.yandex.ru/");
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
