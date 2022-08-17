using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration
{
    public class TokenValidationConfigurationSource : IConfigurationSource
    {
        private readonly MultifactorHttpClientAdapterFactory _clientFactory;

        public TokenValidationConfigurationSource(MultifactorHttpClientAdapterFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new MultiFactorConfigurationProvider(_clientFactory);        
    }
}