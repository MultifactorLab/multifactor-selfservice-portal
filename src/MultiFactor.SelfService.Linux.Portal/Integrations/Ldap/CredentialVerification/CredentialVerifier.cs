using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification
{
    public class CredentialVerifier
    {
        private readonly LdapConnectionAdapterFactory _connectionFactory;
        private readonly PortalSettings _settings;
        private readonly ILogger<CredentialVerifier> _logger;
        private readonly LdapProfileLoader _profileLoader;

        public CredentialVerifier(LdapConnectionAdapterFactory connectionFactory, PortalSettings settings,
            ILogger<CredentialVerifier> logger, LdapProfileLoader profileLoader)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
        }

        public async Task<CredentialVerificationResult> VerifyCredentialAsync(string username, string password)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (password is null)
            {
                _logger.LogError("Empty password provided for user '{user:l}'", username);
                return CredentialVerificationResult.FromUnknowError("Invalid credentials");
            };

            try
            {
                using var connection = await _connectionFactory.CreateAdapterAsync(username, password);
                if (connection.BindedUser == null) throw new Exception("Binded user is not defined. Maybe anonymous connection?");

                var user = connection.BindedUser;
                var domain = await connection.WhereAmIAsync();

                var profile = await _profileLoader.LoadProfileAsync(domain, user, connection);
                if (profile == null)
                {
                    return CredentialVerificationResult.FromUnknowError("Unable to load profile");
                }

                if (!string.IsNullOrEmpty(_settings.ActiveDirectorySettings.SecondFactorGroup))
                {
                    if (!IsMemberOf(profile, _settings.ActiveDirectorySettings.SecondFactorGroup))
                    {
                        _logger.LogInformation("User '{user:l}' is not member of {2FaGroup:l} group", user, _settings.ActiveDirectorySettings.SecondFactorGroup);
                        _logger.LogInformation("Bypass second factor for user '{user:l}'", user);
                        return CredentialVerificationResult.ByPass();
                    }
                    _logger.LogInformation("User '{user:l}' is member of {2FaGroup:l} group", user, _settings.ActiveDirectorySettings.SecondFactorGroup);
                }

                var resultBuilder = CredentialVerificationResult.CreateBuilder(true)
                    .SetDisplayName(profile.DisplayName)
                    .SetEmail(profile.Email);

                if (_settings.ActiveDirectorySettings.UseUserPhone)
                {
                    resultBuilder.SetPhone(profile.Phone);
                }
                if (_settings.ActiveDirectorySettings.UseMobileUserPhone)
                {
                    resultBuilder.SetPhone(profile.Mobile);
                }

                return resultBuilder.Build();
            }
            catch (LdapException ex)
            {
                if (ex.Message != null)
                {
                    var result = CredentialVerificationResult.FromKnownError(ex.Message);
                    _logger.LogWarning("Verification user '{user:l}' at {Domain:l} failed: {Reason:l}. Error message: {msg:l}",
                        username, _settings.CompanySettings.Domain, result.Reason, ex.Message);
                    return result;
                }

                _logger.LogError(ex, "Verification user '{user:l}' at {Domain:l} failed: {msg:l}", 
                    username, _settings.CompanySettings.Domain, ex.Message);
                return CredentialVerificationResult.FromUnknowError(ex.Message);
            }
            catch (Exception ex) when (ex is TechnicalAccountErrorException || ex is LdapUserNotFoundException)
            {
                _logger.LogError(ex, "Verification user '{user}' at {Domain:l} failed: {msg:l}", 
                    username, _settings.CompanySettings.Domain, ex.Message);
                return CredentialVerificationResult.FromUnknowError("Invalid credentials");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verification user '{user:l}' at {Domain:l} failed: {msg:l}", 
                    username, _settings.CompanySettings.Domain, ex.Message);
                return CredentialVerificationResult.FromUnknowError(ex.Message);
            }
        }

        private bool IsMemberOf(LdapProfile profile, string group)
        {
            return profile.MemberOf.Any(g => g.ToLower() == group.ToLower().Trim());
        }
    }
}
