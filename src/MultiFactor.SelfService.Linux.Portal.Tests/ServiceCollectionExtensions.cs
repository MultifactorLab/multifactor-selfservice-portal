using Microsoft.Extensions.DependencyInjection;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RemoveService<TService>(this IServiceCollection services) where TService : class
        {
            var descr = services.FirstOrDefault(x => x.ServiceType == typeof(TService));
            services.Remove(descr);
            return services;
        }
    }
}
