﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
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
                .AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.SaveToken = true;
                    x.TokenValidationParameters = TokenValidationParametersFactory.GetParameters(applicationBuilder.Configuration);
                });

            return applicationBuilder;
        }
    }
}
