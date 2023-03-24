﻿using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Core.Caching
{
    public static class CacheExtensions
    {
        public static IServiceCollection AddPasswordChangingSessionCache(this IServiceCollection services)
        {
            var settings = services.BuildServiceProvider().GetRequiredService<PortalSettings>().PasswordChangingSessionSettings;
            services.AddMemoryCache(x =>
            {
                // 5 Mb by default
                x.SizeLimit = settings.PwdChangingSessionCacheSize ?? 1024 * 1024 * 5;
            });

            services.Configure<ApplicationCacheConfig>(x =>
            {
                if (settings.PwdChangingSessionLifetime != null)
                {
                    x.AbsoluteExpiration = settings.PwdChangingSessionLifetime.Value;
                }
            });
            services.AddSingleton<ApplicationCache>();
            return services;
        }
    }
}