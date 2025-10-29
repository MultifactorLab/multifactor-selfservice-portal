namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration.Providers
{
    public interface ICloudConfigurationRefresher
    {
        void Refresh();
    }

    public class CloudConfigurationRefresher : ICloudConfigurationRefresher
    {
        private readonly CloudConfigurationProvider _provider;

        public CloudConfigurationRefresher(CloudConfigurationProvider provider)
        {
            _provider = provider;
        }

        public void Refresh()
        {
            _provider.Load();
        }
    }
}
