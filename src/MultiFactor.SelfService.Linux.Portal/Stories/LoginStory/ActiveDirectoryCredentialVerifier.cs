using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoginStory
{
    public class ActiveDirectoryCredentialVerifier
    {
        private readonly PortalSettings _settings;

        public ActiveDirectoryCredentialVerifier(PortalSettings settings)
        {
            _settings = settings;
        }

        public async Task<CredentialVerificationResult> VerifyCredentialAsync(string username, string password)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));
            if (password is null)
            {
                // _logger.Error("Empty password provided for user '{user:l}'", userName);
                return CredentialVerificationResult.FromUnknowError("Invalid credentials");
            };

            var user = LdapIdentity.ParseUser(username);

            try
            {
                using var connection = await ActiveDirectoryConnection.CreateAsync("dc1.multifactor.dev", user, password);

                var domain = await connection.WhereAmI();
                var profileLoader = new ActiveDirectoryProfileLoader(connection);
                var profile = await profileLoader.LoadProfileAsync(domain, user);
                if (profile == null)
                {
                    return CredentialVerificationResult.FromUnknowError("Unable to load profile");
                }

                if (!string.IsNullOrEmpty(_settings.ActiveDirectory2FaGroup))
                {
                    if (!IsMemberOf(profile, _settings.ActiveDirectory2FaGroup))
                    {
                        //_logger.Information($"User '{{user:l}}' is not member of {_configuration.ActiveDirectory2FaGroup} group", user.Name);
                        //_logger.Information("Bypass second factor for user '{user:l}'", user.Name);
                        return CredentialVerificationResult.ByPass();
                    }
                    //_logger.Information($"User '{{user:l}}' is member of {_configuration.ActiveDirectory2FaGroup} group", user.Name);
                }

                var resultBuilder = CredentialVerificationResult.CreateBuilder(true)
                    .SetDisplayName(profile.DisplayName)
                    .SetEmail(profile.Email);

                if (_settings.UseActiveDirectoryUserPhone)
                {
                    resultBuilder.SetPhone(profile.Phone);
                }
                if (_settings.UseActiveDirectoryMobileUserPhone)
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
                    //_logger.Warning($"Verification user '{{user:l}}' at {_configuration.Domain} failed: {result.Reason}", user.Name);
                    return result;
                }
                //_logger.Error(lex, $"Verification user '{{user:l}}' at {_configuration.Domain} failed", user.Name);
                return CredentialVerificationResult.FromUnknowError(ex.Message);
            }
            catch (Exception ex)
            {
                //_logger.Error(ex, $"Verification user '{{user:l}}' at {_configuration.Domain} failed.", user.Name);
                return CredentialVerificationResult.FromUnknowError(ex.Message);
            }
        }

        private bool IsMemberOf(ActiveDirectoryProfile profile, string group)
        {
            return profile.MemberOf.Any(g => g.ToLower() == group.ToLower().Trim());
        }
    }
}
