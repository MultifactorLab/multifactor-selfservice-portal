﻿using LdapForNet;
using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.PasswordChanging
{
    public class UserPasswordChanger
    {
        private readonly LdapConnectionAdapterFactory _connectionFactory;
        private readonly IPasswordAttributeChanger _passwordAttributeChanger;
        private readonly ILogger _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly LdapProfileLoader _profileLoader;

        public UserPasswordChanger(LdapConnectionAdapterFactory connectionFactory,
            ILogger<UserPasswordChanger> logger,
            IPasswordAttributeChanger passwordAttributeChanger,
            IStringLocalizer<SharedResource> localizer,
            LdapProfileLoader profileLoader)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _passwordAttributeChanger = passwordAttributeChanger ?? throw new ArgumentNullException(nameof(passwordAttributeChanger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _profileLoader = profileLoader ?? throw new ArgumentNullException(nameof(profileLoader));
        }

        public async Task<PasswordChangingResult> ChangePassword(
            string username, 
            string currentPassword, 
            string newPassword, 
            ChangePasswordMode passwordChangeMode)
        {
            try
            {
                using var connection = passwordChangeMode == ChangePasswordMode.AsTechnicalAccount
                 ? await _connectionFactory.CreateAdapterAsTechnicalAccAsync()
                 : await _connectionFactory.CreateAdapterAsync(username, currentPassword);

                var profile = await _profileLoader.GetLdapProfile(connection, username);

                await _passwordAttributeChanger.ExecuteChangeCommandAsync(profile.DistinguishedName, currentPassword, newPassword, connection);
                
                _logger.LogInformation($"Password changed/reset for user '{username}'");
                return new PasswordChangingResult(true, string.Empty);
            }
            catch (LdapUnwillingToPerformException ex)
            {
                _logger.LogWarning("Change/reset password for user '{username}' failed: {message:l}, {result:l}",
                    username, ex.Message, ex.HResult);
                return new PasswordChangingResult(false, _localizer.GetString("AD.PasswordDoesNotMeetRequirements"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Change/reset password for user '{username}' failed: {message:l}",
                    username, ex.Message);
                return new PasswordChangingResult(false, _localizer.GetString("AD.UnableToChangePassword"));
            }
        }
    }
}
