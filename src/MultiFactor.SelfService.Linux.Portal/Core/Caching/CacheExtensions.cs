using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Core.Caching
{
    public static class CacheExtensions
    {
        public static IServiceCollection AddPasswordChangingSessionCache(this IServiceCollection services)
        {
            var settings = services.BuildServiceProvider().GetRequiredService<PortalSettings>().PasswordManagement!;
            services.AddMemoryCache(x =>
            {
                // 5 Mb by default
                x.SizeLimit = settings.PasswordChangingSessionCacheSize ?? 1024 * 1024 * 5;
            });

            services.Configure<ApplicationCacheConfig>(x =>
            {
                if (settings.PasswordChangingSessionLifetime != null)
                {
                    x.AbsoluteExpiration = settings.PasswordChangingSessionLifetime.Value;
                }
            });
            services.AddSingleton<ApplicationCache>();
            return services;
        }
    }
}
