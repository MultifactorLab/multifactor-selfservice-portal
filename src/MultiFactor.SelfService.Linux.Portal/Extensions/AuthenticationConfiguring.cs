using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication;
using MultiFactor.SelfService.Linux.Portal.Core.Configuration.Providers;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class AuthenticationConfiguring
    {
        /// <summary>
        /// Configures authentication services and rules using ServiceProvider.
        /// Must be called after the application services are configured (ConfigureApplicationServices());
        /// </summary>
        /// <param name="applicationBuilder">A builder for web application and services.</param>
        public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Host.ConfigureAppConfiguration((hostingContext, configBuilder) =>
            {
                var provider = applicationBuilder.Services.BuildServiceProvider();
                configBuilder.Add(new TokenValidationConfigurationProvider(
                    provider.GetRequiredService<MultifactorHttpClientAdapterFactory>(), 
                    provider.GetRequiredService<ILogger<TokenValidationConfigurationProvider>>()));
            });

            applicationBuilder.Services
                .AddAntiforgery()
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "Hybrid";
                    options.DefaultChallengeScheme = "Hybrid";
                })
                .AddPolicyScheme("Hybrid", "JWT or Kerberos", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var auth = context.Request.Headers.Authorization.ToString();

                        if (!string.IsNullOrEmpty(auth))
                        {
                            if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                return JwtBearerDefaults.AuthenticationScheme;

                            if (auth.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
                                return NegotiateDefaults.AuthenticationScheme;
                        }

                        // default → JWT
                        return JwtBearerDefaults.AuthenticationScheme;
                    };
                })
                .AddJwtBearer(x =>
                {
                    x.SaveToken = true;
                    x.TokenValidationParameters = TokenValidationParametersFactory.GetParameters(applicationBuilder.Configuration);
                    x.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (string.IsNullOrEmpty(context.Token))
                            {
                                var cookie = context.Request.Cookies[Constants.COOKIE_NAME];
                                if (!string.IsNullOrEmpty(cookie))
                                    context.Token = cookie;
                            }

                            return Task.CompletedTask;
                        },
                        
                        OnAuthenticationFailed = context =>
                        {
                            context.NoResult();
                            return Task.CompletedTask;
                        }
                    };
                })
                .AddNegotiate(options =>
                {
                    options.Events = new NegotiateEvents
                    {
                        OnAuthenticated = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("NegotiateAuthentication");

                            var authType = context.Principal?.Identity?.AuthenticationType;
                            
                            if (string.Equals(authType, "NTLM", StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogWarning("NTLM authentication rejected");
                                context.Fail("Only Kerberos authentication is allowed.");
                                return Task.CompletedTask;
                            }
                            
                            if (OperatingSystem.IsWindows())
                            {
                                if (!string.Equals(authType, "Kerberos", StringComparison.OrdinalIgnoreCase))
                                {
                                    logger.LogWarning(
                                        "Unsupported auth type: {AuthType}", authType);

                                    context.Fail("Only Kerberos authentication is allowed.");
                                    return Task.CompletedTask;
                                }
                            }

                            logger.LogDebug("Kerberos auth success: {AuthType}", authType);

                            return Task.CompletedTask;
                        },

                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("NegotiateAuthentication");

                            logger.LogWarning(context.Exception, "Negotiate authentication failed");
                            return Task.CompletedTask;
                        }
                    };
                });

            return applicationBuilder;
        }
    }
}
