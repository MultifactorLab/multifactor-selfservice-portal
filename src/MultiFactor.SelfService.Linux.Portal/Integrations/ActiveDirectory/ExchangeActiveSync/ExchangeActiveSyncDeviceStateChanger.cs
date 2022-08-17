using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync.Models;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using LdapForNet;
using static LdapForNet.Native.Native;
using FluentValidation;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync
{
    public class ExchangeActiveSyncDeviceStateChanger
    {
        private readonly PortalSettings _settings;
        private readonly ILogger<ExchangeActiveSyncDeviceStateChanger> _logger;

        public ExchangeActiveSyncDeviceStateChanger(PortalSettings settings, ILogger<ExchangeActiveSyncDeviceStateChanger> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public async Task ChangeStateAsync(ExchangeActiveSyncDeviceInfo device, ExchangeActiveSyncDeviceAccessState state)
        {
            ValidateDeviceInfo(device);

            var techUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User);

            try
            {
                using var connection = await LdapConnectionAdapter.CreateAsync(_settings.CompanySettings.Domain, techUser, _settings.TechnicalAccountSettings.Password, _logger);

                // first, we need to update device state and state reason.
                // modify attributes msExchDeviceAccessState and msExchDeviceAccessStateReason
                await UpdateDeviceAttributesAsync(device, state, connection);

                // then update user msExchMobileAllowedDeviceIDs and msExchMobileBlockedDeviceIDs attributes
                await UpdateUserAttributesAsync(device, state, connection);

                _logger.LogInformation("Exchange ActiveSync device '{device}' {state}", device, state);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Update Exchange ActiveSync device '{device}' failed: {msg:l}", device, ex.Message);
            }
        }

        private static async Task UpdateDeviceAttributesAsync(ExchangeActiveSyncDeviceInfo device, ExchangeActiveSyncDeviceAccessState state, LdapConnectionAdapter connection)
        {
            var stateModificator = new DirectoryModificationAttribute
            {
                Name = "msExchDeviceAccessState",
                LdapModOperation = LdapModOperation.LDAP_MOD_REPLACE,
            };
            stateModificator.Add(state.ToString("d"));

            var stateReasonModificator = new DirectoryModificationAttribute
            {
                Name = "msExchDeviceAccessStateReason",
                LdapModOperation = LdapModOperation.LDAP_MOD_REPLACE,
            };
            stateReasonModificator.Add("2"); // individual

            await connection.SendRequestAsync(new ModifyRequest(device.DeviceDistinguishedName, stateModificator, stateReasonModificator));
        }

        private static async Task UpdateUserAttributesAsync(ExchangeActiveSyncDeviceInfo device, ExchangeActiveSyncDeviceAccessState state, LdapConnectionAdapter connection)
        {
            var allowedModificator = new DirectoryModificationAttribute
            {
                Name = "msExchMobileAllowedDeviceIDs",
                LdapModOperation = state == ExchangeActiveSyncDeviceAccessState.Allowed ? LdapModOperation.LDAP_MOD_ADD : LdapModOperation.LDAP_MOD_DELETE
            };
            allowedModificator.Add(device.DeviceId);

            var blockedModificator = new DirectoryModificationAttribute
            {
                Name = "msExchMobileBlockedDeviceIDs",
                LdapModOperation = state == ExchangeActiveSyncDeviceAccessState.Blocked ? LdapModOperation.LDAP_MOD_ADD : LdapModOperation.LDAP_MOD_DELETE
            };
            blockedModificator.Add(device.DeviceId);

            var modifyRequest = new ModifyRequest(device.UserDistinguishedName, allowedModificator, blockedModificator);
            // ignore if it attempts to add an attribute that already exists or if it attempts to delete an attribute that does not exist
            modifyRequest.Controls.Add(new PermissiveModifyControl());

            await connection.SendRequestAsync(modifyRequest);
        }

        private static void ValidateDeviceInfo(ExchangeActiveSyncDeviceInfo deviceInfo)
        {
            if (deviceInfo is null) throw new ArgumentNullException(nameof(deviceInfo));

            var result = new DeviceInfoValidator().Validate(deviceInfo);
            if (result.IsValid) return;

            throw new Exception($"Update Exchange ActiveSync device '{deviceInfo}' failed: {result.Errors.FirstOrDefault()?.ErrorMessage}");
        }

        private class DeviceInfoValidator : AbstractValidator<ExchangeActiveSyncDeviceInfo>
        {
            public DeviceInfoValidator()
            {
                RuleFor(c => c.DeviceId).NotEmpty().WithMessage(GetErrorMessage(nameof(ExchangeActiveSyncDeviceInfo.DeviceId)));
                RuleFor(c => c.UserDistinguishedName).NotEmpty().WithMessage(GetErrorMessage(nameof(ExchangeActiveSyncDeviceInfo.UserDistinguishedName)));
                RuleFor(c => c.DeviceDistinguishedName).NotEmpty().WithMessage(GetErrorMessage(nameof(ExchangeActiveSyncDeviceInfo.DeviceDistinguishedName)));
            }

            private static string GetErrorMessage(string propertyName)
            {
                return $"Incorrect device info: '{propertyName}' is required";
            }
        }
    }
}
