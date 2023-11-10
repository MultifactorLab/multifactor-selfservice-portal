using MultiFactor.SelfService.Linux.Portal.Core.Metadata;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    internal class ApplicationStarter : IHostedService
    {
        private readonly LdapConnectionAdapterFactory _connectionAdapterFactory;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<ApplicationStarter> _logger;
        private readonly AdditionalClaimsMetadata _claimsMetadata;

        public ApplicationStarter(LdapConnectionAdapterFactory connectionAdapterFactory, IHostApplicationLifetime lifetime, 
            ILogger<ApplicationStarter> logger,
            AdditionalClaimsMetadata claimsMetadata)
        {
            _connectionAdapterFactory = connectionAdapterFactory ?? throw new ArgumentNullException(nameof(connectionAdapterFactory));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claimsMetadata = claimsMetadata ?? throw new ArgumentNullException(nameof(claimsMetadata));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                LoadAdditionalClaimsMetadata();
                await CheckLdapConnection();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unable to start application: {msg:l}", ex.Message);
                _lifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask; 
        
        private void LoadAdditionalClaimsMetadata()
        {
            try
            {
                _claimsMetadata.LoadMetadata();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failde to load claims metadata: {ex.Message}", ex);
            }
        }
        
        private async Task CheckLdapConnection()
        {
            _logger.LogInformation("Checking tech user credentials...");
            try
            {
                using var connection = await _connectionAdapterFactory.CreateAdapterAsTechnicalAccAsync();
            } 
            catch (Exception ex) 
            {
                throw new Exception($"Failde to check tech user credentials: {ex.Message}", ex);
            }
            _logger.LogInformation("Tech user redentials are OK");
        }
    }
}
