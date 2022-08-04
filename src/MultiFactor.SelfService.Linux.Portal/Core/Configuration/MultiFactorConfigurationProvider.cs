using MultiFactor.SelfService.Linux.Portal.Core.Http;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration
{
    public class MultiFactorConfigurationProvider : ConfigurationProvider
    {
        private readonly ApplicationHttpClient _client;

        public MultiFactorConfigurationProvider(ApplicationHttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override void Load()
        {
            var value = _client.GetAsync(".well-known/jwks.json").Result ?? "{\"keys\":[]}";
            Data = new Dictionary<string, string>
            {
                { Constants.TOKEN_VALIDATION, value }
            };
        }
    }
}
