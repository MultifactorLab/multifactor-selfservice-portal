using MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeActiveSyncDeviceStateStory;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeExpiredPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Stories.ChangeValidPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Stories.CheckExpiredPasswordSessionStory;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory;
using MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SearchExchangeActiveSyncDevicesStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignInStory.ClaimsSources;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Google;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Google.ReCaptcha;
using MultiFactor.SelfService.Linux.Portal.Integrations.Captcha.Yandex;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using System.Net;
using MultiFactor.SelfService.Linux.Portal.Stories.RecoverPasswordStory;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;

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
                .AddSingleton<ILdapConnectionAdapter, LdapConnectionAdapter>()
                .AddSingleton<SafeHttpContextAccessor>()
                .AddSingleton<TokenVerifier>()
                .AddSingleton<TokenClaimsAccessor>()
                .AddSingleton<DataProtection>()
                .AddSingleton<JsonDataSerializer>()
                .AddSingleton<JsonPayloadLogger>()
                .AddSingleton<DeviceAccessStateNameLocalizer>()
                .AddSingleton<CredentialVerifier>()
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
                .AddSingleton<IPasswordAttributeReplacer, ADPasswordAttributeReplacer>()
                .AddSingleton<UserPasswordChanger>()
                .AddSingleton<ForgottenPasswordChanger>()

                .AddSingleton<ILdapProfileFilterProvider, LdapProfileFilterProvider>()
                .AddSingleton<LdapProfileLoader>()
                
                .AddTransient<HttpMessageInterceptor>()
                .AddTransient<ICaptchaVerifier, GoogleReCaptchaVerifier>()

                .AddTransient<SignInStory>()
                .AddTransient<IdentityStory>()
                .AddTransient<RedirectToCredValidationAfter2faStory>()
                .AddTransient<AuthnStory>()
                .AddTransient<SignOutStory>()
                .AddTransient<LoadProfileStory>()
                .AddTransient<RecoverPasswordStory>()
                .AddTransient<AuthenticateSessionStory>()
                .AddTransient<GetApplicationInfoStory>()
                .AddTransient<CheckExpiredPasswordSessionStory>()
                .AddTransient<ChangeExpiredPasswordStory>()
                .AddTransient<ChangeValidPasswordStory>()
                .AddTransient<SearchExchangeActiveSyncDevicesStory>()
                .AddTransient<ChangeActiveSyncDeviceStateStory>();
            
            ConfigureHttpClients(builder);
           
            ConfigureMultifactorApi(builder);
            ConfigureGoogleApi(builder);
            ConfigureYandexCaptchaApi(builder);

            ConfigureCaptchaVerifier(builder);

            builder.Services.AddHostedService<ApplicationChecker>();

            builder.Services
                .AddSingleton<ClaimsProvider>()
                .AddSingleton<IClaimsSource, MultiFactorClaimsSource>()
                .AddSingleton<IClaimsSource, SsoClaimsSource>()
                .AddSingleton<IClaimsSource, AdditionalClaimsSource>();

            builder.Services.AddSingleton<AdditionalClaimDescriptorsProvider>();
            builder.Services.AddSingleton<AdditionalClaimsMetadata>();

            builder.Services.AddSingleton<IApplicationValuesContext, ClaimValuesContext>();
            builder.Services.AddSingleton<ApplicationGlobalValuesProvider>();
            builder.Services.AddSingleton<ClaimConditionEvaluator>();

            builder.Services.AddSingleton<ContentCache>();

            return builder;
        }

        private static void ConfigureHttpClients(WebApplicationBuilder builder)
        {
            var handler = new HttpClientHandler();
            var proxySetting = builder.Configuration.GetPortalSettingsValue(x => x.MultiFactorApiSettings.ApiProxy);
            if (!string.IsNullOrEmpty(proxySetting))
            {
                handler.Proxy = BuildProxy(proxySetting);
            }

            builder.Services
                .AddHttpClient(Constants.HttpClients.MultifactorApi, (services, client) =>
                {
                    var settings = services.GetRequiredService<PortalSettings>();
                    client.BaseAddress = new Uri(settings.MultiFactorApiSettings.ApiUrl!);
                    
                })
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddHttpMessageHandler<HttpMessageInterceptor>();
            
            builder.Services
                .AddHttpClient(Constants.HttpClients.GoogleCaptcha, client =>
                {
                    client.BaseAddress = new Uri("https://www.google.com/recaptcha/api/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddHttpMessageHandler<HttpMessageInterceptor>();
            
            builder.Services
                .AddHttpClient(Constants.HttpClients.YandexCaptcha, client =>
                {
                    client.BaseAddress = new Uri("https://captcha-api.yandex.ru/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddHttpMessageHandler<HttpMessageInterceptor>();
            
            builder.Services
                .AddHttpClient(Constants.HttpClients.MultifactorIdpApi)
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddHttpMessageHandler<HttpMessageInterceptor>();
        }
        

        private static void ConfigureMultifactorApi(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<MultiFactorApi>()
                .AddTransient<MultifactorHttpClientAdapterFactory>();
        }

        private static void ConfigureGoogleApi(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<GoogleReCaptcha2Api>()
                .AddTransient<GoogleHttpClientAdapterFactory>();
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
                .AddTransient<YandexHttpClientAdapterFactory>();
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
