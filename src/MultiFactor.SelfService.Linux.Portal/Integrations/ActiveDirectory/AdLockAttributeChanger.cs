using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory;

public class AdLockAttributeChanger : ILockAttributeChanger
{
    private readonly IUserAttributeChanger _userAttributeChanger;
    private readonly LdapConnectionAdapterFactory _connectionFactory;
    private readonly LdapProfileLoader _profileLoader;
    private readonly ILogger _logger;

    public string AttributeName => "lockoutTime";

    public AdLockAttributeChanger(
        IUserAttributeChanger userAttributeChanger,
        LdapConnectionAdapterFactory connectionAdapterFactory,
        LdapProfileLoader profileLoader,
        ILogger<ILockAttributeChanger> logger)
    {
        _userAttributeChanger = userAttributeChanger;
        _connectionFactory = connectionAdapterFactory;
        _profileLoader = profileLoader;
        _logger = logger;
    }

    public async Task<bool> ChangeLockAttributeValue(string userName, object value)
    {
        try
        {
            var connectionAdapter = await _connectionFactory.CreateAdapterAsTechnicalAccAsync();

            var profile = await _profileLoader.GetLdapProfile(connectionAdapter, userName);
            await _userAttributeChanger.ChangeAttributeValueAsync(
                profile.DistinguishedName,
                AttributeName,
                value,
                connectionAdapter);
            
            _logger.LogInformation("User '{username}' is unlocked", userName);

            return true;
        }
        catch (LdapUnwillingToPerformException ex)
        {
            _logger.LogWarning("Unlock operation for user '{username}' failed: {message:l}, {result:l}",
                userName, ex.Message, ex.HResult);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Unlock operation for user '{username}' failed: {message:l}",
                userName, ex.Message);
            return false;
        }
    }
}