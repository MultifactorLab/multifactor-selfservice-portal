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
                x.SizeLimit = settings.PasswordChangingSession.CacheSize ?? 1024 * 1024 * 5;
            });

            services.Configure<ApplicationCacheConfig>(x =>
            {
                if (settings.PasswordChangingSession.Lifetime != null)
                {
                    x.AbsoluteExpiration = settings.PasswordChangingSession.Lifetime.Value;
                }
            });
            services.AddSingleton<ApplicationCache>();
            return services;
        }
    }
}
