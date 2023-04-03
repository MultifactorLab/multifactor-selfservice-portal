namespace MultiFactor.SelfService.Linux.Portal.Core.Caching
{
    public class ApplicationCacheConfig
    {
        public TimeSpan AbsoluteExpiration { get; set; } = TimeSpan.FromMinutes(2);
    }
}
