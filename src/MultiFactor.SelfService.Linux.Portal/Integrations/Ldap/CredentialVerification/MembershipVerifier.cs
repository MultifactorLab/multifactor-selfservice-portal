using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;

public class MembershipVerifier
{
    private readonly LdapConnectionAdapterFactory _connectionFactory;
    private readonly PortalSettings _settings;
    private readonly ILogger<CredentialVerifier> _logger;
    private readonly LdapProfileLoader _profileLoader;
    private readonly SafeHttpContextAccessor _httpContextAccessor;

    public MembershipVerifier(LdapConnectionAdapterFactory connectionFactory, 
        PortalSettings settings,
        ILogger<CredentialVerifier> logger, LdapProfileLoader profileLoader,
        SafeHttpContextAccessor httpContextAccessor)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Checks credentials and set CredentialVerificationResult to the HttpContext.
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task VerifyMembershipAsync(string username, string password)
    {
        using var connection = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();
        
    }
}