using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification
{
    /// <summary>
    /// First factor credentials verifier.
    /// </summary>
    public class CredentialVerifier
    {
        private readonly LdapConnectionAdapterFactory _connectionFactory;
        private readonly PortalSettings _settings;
        private readonly ILogger<CredentialVerifier> _logger;
        private readonly LdapProfileLoader _profileLoader;
        private readonly SafeHttpContextAccessor _httpContextAccessor;

        public CredentialVerifier(
            LdapConnectionAdapterFactory connectionFactory,
            PortalSettings settings,
            ILogger<CredentialVerifier> logger,
            LdapProfileLoader profileLoader,
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
        public async Task<CredentialVerificationResult> VerifyCredentialAsync(string username, string password)
        {
            ArgumentNullException.ThrowIfNull(username);
            if (password is null)
            {
                _logger.LogError("Empty password provided for user '{user:l}'", username);
                return CredentialVerificationResult.FromUnknownError("Invalid credentials");
            }

            try
            {
                if (_settings.NeedPrebindInfo())
                {
                    await VerifyCredentialOnlyAsync(username, password);
                    return await VerifyMembership(username);
                }

                using var connection = await _connectionFactory.CreateAdapterAsync(
                    username,
                    password);

                if (connection.BindedUser == null)
                    throw new Exception("Binded user is not defined. Maybe anonymous connection?");

                var user = connection.BindedUser;
                var domain = await connection.WhereAmIAsync();

                var profile = await _profileLoader.LoadProfileAsync(domain, user, connection);
                if (profile == null)
                {
                    return CredentialVerificationResult.FromUnknownError("Unable to load profile");
                }

                _httpContextAccessor.HttpContext.Items[Constants.LoadedLdapAttributes] = profile.Attributes;

                if (_settings.ActiveDirectorySettings.SplittedActiveDirectoryGroups?.Length > 0)
                {
                    if (IsMemberOfActiveDirectorGroups(profile, out var accessGroup))
                    {
                        _logger.LogDebug(
                            "User '{user:l}' is a member of the access group '{group:l}' in {domain:l}",
                            user.Name,
                            accessGroup.Trim(),
                            profile.BaseDn.Name);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "User '{user:l}' is not a member of any access group ({accGroups:l}) in '{domain:l}'",
                            user.Name,
                            string.Join(", ", _settings.ActiveDirectorySettings.SplittedActiveDirectoryGroups),
                            profile.BaseDn.Name);
                        return CredentialVerificationResult.CreateBuilder(false).Build();
                    }
                }

                if (_settings.ActiveDirectorySettings.SecondFactorGroups.Length != 0)
                {
                    var mfaGroup = _settings.ActiveDirectorySettings.SecondFactorGroups.FirstOrDefault(group => IsMemberOf(profile, group));

                    if (mfaGroup == null)
                    {
                        _logger.LogInformation("User '{user:l}' is not member of {2FaGroup:l} group", user,
                            _settings.ActiveDirectorySettings.SecondFactorGroups);
                        return CredentialVerificationResult.ByPass(username, profile.Upn, profile.UserMustChangePassword());
                    }

                    _logger.LogInformation("User '{user:l}' is member of {2FaGroup:l} group", user,
                        _settings.ActiveDirectorySettings.SecondFactorGroups);
                }

                var resultBuilder = CredentialVerificationResult.CreateBuilder(true)
                    .SetDisplayName(profile.DisplayName)
                    .SetEmail(profile.Email)
                    .SetUserMustChangePassword(profile.UserMustChangePassword())
                    .SetPasswordExpirationDate(profile.PasswordExpirationDate());

                if (_settings.ActiveDirectorySettings.UseUserPhone)
                {
                    resultBuilder.SetPhone(profile.Phone);
                }

                if (_settings.ActiveDirectorySettings.UseMobileUserPhone)
                {
                    resultBuilder.SetPhone(profile.Mobile);
                }

                if (_settings.ActiveDirectorySettings.UseUpnAsIdentity)
                {
                    resultBuilder.SetUserPrincipalName(profile.Upn);
                }

                var identityAttribute = _settings.ActiveDirectorySettings.UseAttributeAsIdentity;
                if (!string.IsNullOrWhiteSpace(identityAttribute))
                {
                    resultBuilder.OverrideIdentity(profile.Attributes.GetValue(identityAttribute));
                }

                resultBuilder.SetUsername(username);

                var result = resultBuilder.Build();
                _httpContextAccessor.HttpContext.Items[Constants.CredentialVerificationResult] = result;

                return result;
            }
            catch (LdapException ex)
            {
                if (ex.Message != null)
                {
                    var result = CredentialVerificationResult.FromKnownError(ex.Message, username);

                    _logger.LogWarning(
                        "Verification user '{user:l}' at {Domain:l} failed: {Reason:l}. Error message: {msg:l}",
                        username, _settings.CompanySettings.Domain, result.Reason, ex.Message);

                    _httpContextAccessor.HttpContext.Items[Constants.CredentialVerificationResult] = result;
                    return result;
                }

                _logger.LogError(ex, "Verification user '{user:l}' at {Domain:l} failed: {msg:l}",
                    username, _settings.CompanySettings.Domain, ex.Message);

                var res = CredentialVerificationResult.FromUnknownError(ex.Message);
                _httpContextAccessor.HttpContext.Items[Constants.CredentialVerificationResult] = res;
                return res;
            }
            catch (Exception ex) when (ex is TechnicalAccountErrorException or LdapUserNotFoundException)
            {
                _logger.LogError(ex, "Verification user '{user}' at {Domain:l} failed: {msg:l}",
                    username, _settings.CompanySettings.Domain, ex.Message);

                var res = CredentialVerificationResult.FromUnknownError("Invalid credentials");
                _httpContextAccessor.HttpContext.Items[Constants.CredentialVerificationResult] = res;
                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verification user '{user:l}' at {Domain:l} failed: {msg:l}",
                    username, _settings.CompanySettings.Domain, ex.Message);

                var res = CredentialVerificationResult.FromUnknownError(ex.Message);
                _httpContextAccessor.HttpContext.Items[Constants.CredentialVerificationResult] = res;
                return res;
            }
        }

        public async Task VerifyCredentialOnlyAsync(string username, string password)
        {
            _logger.LogDebug("Verifying user '{User:l}' credential and status", username);

            using var connection = await _connectionFactory.CreateAdapterAsync(username, password);
            var user = connection.BindedUser;
            var domain = await connection.WhereAmIAsync();
            _logger.LogInformation("User '{User:l}' credential and status verified successfully in {Domain:l}",
                username, domain);
        }

        public async Task<CredentialVerificationResult> VerifyMembership(string username)
        {
            using var connection = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();

            var user = LdapIdentity.ParseUser(username);
            var domain = await connection.WhereAmIAsync();

            var profile = await _profileLoader.LoadProfileAsync(domain, user, connection);
            if (profile == null)
            {
                return CredentialVerificationResult.FromUnknownError("Unable to load profile");
            }

            _httpContextAccessor.HttpContext.Items[Constants.LoadedLdapAttributes] = profile.Attributes;

            if (_settings.ActiveDirectorySettings.SplittedActiveDirectoryGroups?.Length > 0)
            {
                if (IsMemberOfActiveDirectorGroups(profile, out var accessGroup))
                {
                    _logger.LogDebug(
                        "User '{user:l}' is a member of the access group '{group:l}' in {domain:l}",
                        user.Name,
                        accessGroup.Trim(),
                        profile.BaseDn.Name);
                }
                else
                {
                    _logger.LogWarning(
                        "User '{user:l}' is not a member of any access group ({accGroups:l}) in '{domain:l}'",
                        user.Name,
                        string.Join(", ", _settings.ActiveDirectorySettings.SplittedActiveDirectoryGroups),
                        profile.BaseDn.Name);
                    return CredentialVerificationResult.CreateBuilder(false).Build();
                }
            }

            if (_settings.ActiveDirectorySettings.SecondFactorGroups.Length != 0)
            {
                var mfaGroup = _settings.ActiveDirectorySettings.SecondFactorGroups.FirstOrDefault(group => IsMemberOf(profile, group));
                if (mfaGroup == null)
                {
                    _logger.LogInformation("User '{user:l}' is not member of {2FaGroup:l} group", user,
                        _settings.ActiveDirectorySettings.SecondFactorGroups);
                    return CredentialVerificationResult.ByPass(username, profile.Upn, profile.UserMustChangePassword());
                }

                _logger.LogInformation("User '{user:l}' is member of {2FaGroup:l} group", user,
                    _settings.ActiveDirectorySettings.SecondFactorGroups);
            }

            var resultBuilder = CredentialVerificationResult.CreateBuilder(true)
                .SetDisplayName(profile.DisplayName)
                .SetEmail(profile.Email)
                .SetUserMustChangePassword(profile.UserMustChangePassword())
                .SetPasswordExpirationDate(profile.PasswordExpirationDate());

            if (_settings.ActiveDirectorySettings.UseUserPhone)
            {
                resultBuilder.SetPhone(profile.Phone);
            }

            if (_settings.ActiveDirectorySettings.UseMobileUserPhone)
            {
                resultBuilder.SetPhone(profile.Mobile);
            }

            if (_settings.ActiveDirectorySettings.UseUpnAsIdentity)
            {
                resultBuilder.SetUserPrincipalName(profile.Upn);
            }

            var identityAttribute = _settings.ActiveDirectorySettings.UseAttributeAsIdentity;
            if (!string.IsNullOrWhiteSpace(identityAttribute))
            {
                resultBuilder.OverrideIdentity(profile.Attributes.GetValue(identityAttribute));
            }

            resultBuilder.SetUsername(username);

            var result = resultBuilder.Build();
            _httpContextAccessor.HttpContext.Items[Constants.CredentialVerificationResult] = result;

            return result;
        }

        private bool IsMemberOfActiveDirectorGroups(LdapProfile profile, out string accessGroup)
        {
            accessGroup = _settings.ActiveDirectorySettings.SplittedActiveDirectoryGroups.FirstOrDefault(x => IsMemberOf(profile, x));

            return !string.IsNullOrEmpty(accessGroup);
        }

        private bool IsMemberOf(LdapProfile profile, string group)
        {
            return profile.MemberOf.Any(g => g.Equals(group.ToLower(), StringComparison.CurrentCultureIgnoreCase));
        }
    }
}