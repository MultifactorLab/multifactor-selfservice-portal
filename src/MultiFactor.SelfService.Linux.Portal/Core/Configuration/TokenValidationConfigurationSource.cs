using MultiFactor.SelfService.Linux.Portal.Core.Http;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration
{
    public class TokenValidationConfigurationSource : IConfigurationSource
    {
        private readonly ApplicationHttpClient _client;

        public TokenValidationConfigurationSource(ApplicationHttpClient client)
        {
            _client = client;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => new MultiFactorConfigurationProvider(_client);        
    }
}
