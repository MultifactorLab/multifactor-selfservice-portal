using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync.Models;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Globalization;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync
{
    public class ExchangeActiveSyncDevicesSearcher
    {
        private readonly PortalSettings _settings;
        private readonly DeviceAccessStateNameLocalizer _stateNameLocalizer;
        private readonly ILogger<ExchangeActiveSyncDevicesSearcher> _logger;

        public ExchangeActiveSyncDevicesSearcher(PortalSettings settings, 
            DeviceAccessStateNameLocalizer stateNameLocalizer, 
            ILogger<ExchangeActiveSyncDevicesSearcher> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _stateNameLocalizer = stateNameLocalizer ?? throw new ArgumentNullException(nameof(stateNameLocalizer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<ExchangeActiveSyncDevice>> FindAllByUserAsync(string username)
        {
            if (username is null) throw new ArgumentNullException(nameof(username));

            var user = LdapIdentity.ParseUser(username);
            var techUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User);

            try
            {
                using var connection = await LdapConnectionAdapter.CreateAsync(
                    _settings.CompanySettings.Domain, 
                    techUser, 
                    _settings.TechnicalAccountSettings.Password, 
                    _logger);

                var domain = await connection.WhereAmI();
                var names = new LdapNames(LdapServerType.ActiveDirectory);
                var profileLoader = new LdapProfileLoader(connection, names, _logger);
                var profile = await profileLoader.LoadProfileAsync(domain, user);
                if (profile == null)
                {
                    _logger.LogError("Unable to load profile for user '{user:l}'", username);
                    throw new Exception($"Unable to load profile for user '{username}'");
                }

                _logger.LogDebug("Searching Exchange ActiveSync devices for user '{user:l}' in {fqdn:l}", user.Name, profile.BaseDn.DnToFqdn());
                var filter = $"(objectclass=msexchactivesyncdevice)";
                var attrs = new[]
                {
                    "msExchDeviceID",
                    "msExchDeviceAccessState",
                    "msExchDeviceAccessStateReason",
                    "msExchDeviceFriendlyName",
                    "msExchDeviceModel",
                    "msExchDeviceType",
                    "whenCreated"
                };

                //active sync devices inside user dn container
                var searchResponse = await connection.SearchQueryAsync(profile.DistinguishedName, filter, LdapSearchScope.LDAP_SCOPE_SUB, attrs);
                _logger.LogDebug("Found {count} devices for user '{user:l}'", searchResponse.Count, username);

                return searchResponse.Select(ParseDevice).Where(x => x != null).Cast<ExchangeActiveSyncDevice>().ToList();
                
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Search for user '{user:l}' failed: {ex}", user.Name, ex);
                return new List<ExchangeActiveSyncDevice>();
            }
        }

        public async Task<ExchangeActiveSyncDeviceInfo?> FindByUserAndDeviceIdAsync(string username, string deviceId)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentException($"'{nameof(username)}' cannot be null or empty.", nameof(username));
            if (string.IsNullOrEmpty(deviceId)) throw new ArgumentException($"'{nameof(deviceId)}' cannot be null or empty.", nameof(deviceId));

            var user = LdapIdentity.ParseUser(username);
            var techUser = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User);

            try
            {
                using var connection = await LdapConnectionAdapter.CreateAsync(
                    _settings.CompanySettings.Domain, 
                    techUser, 
                    _settings.TechnicalAccountSettings.Password, 
                    _logger);

                var domain = await connection.WhereAmI();
                var names = new LdapNames(LdapServerType.ActiveDirectory);
                var profileLoader = new LdapProfileLoader(connection, names, _logger);
                var profile = await profileLoader.LoadProfileAsync(domain, user);
                if (profile == null)
                {
                    _logger.LogError("Unable to load profile for user '{user:l}'", username);
                    throw new Exception($"Unable to load profile for user '{username}'");
                }

                _logger.LogDebug("Searching Exchange ActiveSync device {deviceId:l} for user '{user:l}' in {fqdn:l}", deviceId, username, profile.BaseDn.DnToFqdn());

                //active sync device inside user dn container
                var filter = $"(&(objectclass=msexchactivesyncdevice)(msExchDeviceID={deviceId}))";
                var searchResponse = await connection.SearchQueryAsync(profile.DistinguishedName, filter, LdapSearchScope.LDAP_SCOPE_SUB);
                if (searchResponse.Count == 0)
                {
                    _logger.LogWarning("Exchange ActiveSync device {deviceId:l} not found for user '{user:l}' in {fqdn:l}", deviceId, username, profile.BaseDn.DnToFqdn());
                    return null;
                }

                _logger.LogDebug("Found {count} devices for user '{user:l}'", searchResponse.Count, username);

                return new ExchangeActiveSyncDeviceInfo(deviceId, searchResponse.First().Dn, profile.DistinguishedName);

            }
            catch (Exception ex)
            {
                _logger.LogWarning("Search Exchange ActiveSync device {deviceId:l} for user '{user:l}' failed: {message:l}", deviceId, username, ex.Message);
                return null;
            }
        }

        private ExchangeActiveSyncDevice? ParseDevice(LdapEntry entry)
        {
            var attributes = entry.DirectoryAttributes;

            try
            {
                var builder = ExchangeActiveSyncDevice.CreateBuilder();

                if (attributes.TryGetValue("msExchDeviceAccessState", out var msExchDeviceAccessState))
                {
                    var parsed = (ExchangeActiveSyncDeviceAccessState)Convert.ToInt32(msExchDeviceAccessState.GetValue<string>());
                    if (parsed == ExchangeActiveSyncDeviceAccessState.TestActiveSyncConnectivity)
                    {
                        return null;
                    }
                    builder.SetAccessState(parsed);
                    var localized = _stateNameLocalizer.GetLocalizedStateName(parsed);
                    builder.SetAccessStateName(localized);
                }

                if (attributes.TryGetValue("msExchDeviceID", out var msExchDeviceID))
                {
                    builder.SetMsExchDeviceId(msExchDeviceID.GetValue<string>());
                }

                if (attributes.TryGetValue("msExchDeviceAccessStateReason", out var msExchDeviceAccessStateReason))
                {
                    builder.SetAccessStateReason(msExchDeviceAccessStateReason.GetValue<string>());
                }

                if (attributes.TryGetValue("msExchDeviceFriendlyName", out var msExchDeviceFriendlyName))
                {
                    builder.SetFriendlyName(msExchDeviceFriendlyName.GetValue<string>());
                }

                if (attributes.TryGetValue("msExchDeviceModel", out var msExchDeviceModel))
                {
                    builder.SetModel(msExchDeviceModel.GetValue<string>());
                }

                if (attributes.TryGetValue("msExchDeviceType", out var msExchDeviceType))
                {
                    builder.SetType(msExchDeviceType.GetValue<string>());
                }

                if (attributes.TryGetValue("whenCreated", out var whenCreated))
                {
                    builder.SetWhenCreated(ParseLdapDate(whenCreated.GetValue<string>()));
                }

                return builder.Build();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Search for user '{user:l}' succeeded but parsing result failed: {ex}", entry.Dn, ex);
                return null;
            }
        }

        private static DateTime ParseLdapDate(string dateString)
        {
            return DateTime
                .ParseExact(dateString, "yyyyMMddHHmmss.f'Z'", CultureInfo.InvariantCulture)
                .ToLocalTime();
        }
    }
}
