using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.CredentialVerification
{
    public class ActiveDirectoryCredentialVerifier
    {
        private readonly PortalSettings _settings;
        private readonly ILogger<ActiveDirectoryCredentialVerifier> _logger;

        public ActiveDirectoryCredentialVerifier(PortalSettings settings,
            ILogger<ActiveDirectoryCredentialVerifier> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task<CredentialVerificationResult> VerifyCredentialAsync(string username, string password)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (password is null)
            {
                _logger.LogError("Empty password provided for user '{user:l}'", username);
                return CredentialVerificationResult.FromUnknowError("Invalid credentials");
            };

            var user = LdapIdentity.ParseUser(username);

            try
            {
                using var connection = await LdapConnectionAdapter.CreateAsync(_settings.CompanySettings.Domain, user, password, _logger);

                var domain = await connection.WhereAmI();

                var names = new LdapNames(LdapServerType.ActiveDirectory);
                var profileLoader = new LdapProfileLoader(connection, names, _logger);
                var profile = await profileLoader.LoadProfileAsync(domain, user);
                if (profile == null)
                {
                    return CredentialVerificationResult.FromUnknowError("Unable to load profile");
                }

                if (!string.IsNullOrEmpty(_settings.ActiveDirectorySettings.SecondFactorGroup))
                {
                    if (!IsMemberOf(profile, _settings.ActiveDirectorySettings.SecondFactorGroup))
                    {
                        _logger.LogInformation("User '{user:l}' is not member of {2FaGroup:l} group", user.Name, _settings.ActiveDirectorySettings.SecondFactorGroup);
                        _logger.LogInformation("Bypass second factor for user '{user:l}'", user.Name);
                        return CredentialVerificationResult.ByPass();
                    }
                    _logger.LogInformation("User '{user:l}' is member of {2FaGroup:l} group", user.Name, _settings.ActiveDirectorySettings.SecondFactorGroup);
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
                    _logger.LogWarning("Verification user '{user:l}' at {Domain:l} failed: {.Reason:}",
                        user.Name, _settings.CompanySettings.Domain, result.Reason);
                    return result;
                }

                _logger.LogError(ex, "Verification user '{user:l}' at {Domain:l} failed", user.Name, _settings.CompanySettings.Domain);
                return CredentialVerificationResult.FromUnknowError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verification user '{user:l}' at {Domain:l} failed.", user.Name, _settings.CompanySettings.Domain);
                return CredentialVerificationResult.FromUnknowError(ex.Message);
            }
        }

        private bool IsMemberOf(LdapProfile profile, string group)
        {
            return profile.MemberOf.Any(g => g.ToLower() == group.ToLower().Trim());
        }
    }
}
