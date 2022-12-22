using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration
{
    public class TokenValidationConfigurationSource : IConfigurationSource
    {
        private readonly MultifactorHttpClientAdapterFactory _clientFactory;
        private readonly ILogger<TokenValidationConfigurationSource> _logger;

        public TokenValidationConfigurationSource(MultifactorHttpClientAdapterFactory clientFactory, ILogger<TokenValidationConfigurationSource> logger)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _logger = logger;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new MultiFactorConfigurationProvider(_clientFactory, _logger);        
    }
}