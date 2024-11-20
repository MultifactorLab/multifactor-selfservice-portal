namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    internal class ApplicationChecker : IHostedService
    {
        private readonly LdapConnectionAdapterFactory _connectionAdapterFactory;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<ApplicationChecker> _logger;

        public ApplicationChecker(LdapConnectionAdapterFactory connectionAdapterFactory, IHostApplicationLifetime lifetime, ILogger<ApplicationChecker> logger)
        {
            _connectionAdapterFactory = connectionAdapterFactory ?? throw new ArgumentNullException(nameof(connectionAdapterFactory));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Checking tech user credentials...");
                using var connection = await _connectionAdapterFactory.CreateAdapterAsTechnicalAccAsync();
                _logger.LogInformation("Tech user credentials are OK");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unable to start application: {msg:l}", ex.Message);
                _lifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;       
    }
}
