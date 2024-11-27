using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

namespace MultiFactor.SelfService.Linux.Portal.Tests.LoadSettingsTests;

public class ActiveDirectorySettingsTests
{
    [Fact]
    public void ShouldLoadActiveDirectorySettings()
    {
        var settingsPath =
            TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}full-active-directory-settings.xml");
        var portalSetting = TestEnvironment.LoadPortalSettings(settingsPath);
        var adSettings = portalSetting.ActiveDirectorySettings;
        Assert.NotNull(adSettings);
        Assert.True(adSettings.UseUserPhone);
        Assert.True(adSettings.UseMobileUserPhone);
        Assert.False(string.IsNullOrWhiteSpace(adSettings.NetBiosName));
        Assert.True(adSettings.SecondFactorGroups.SequenceEqual(new[] { "SecondFactorGroup1", "SecondFactorGroup2" }));
        Assert.True(adSettings.SplittedActiveDirectoryGroups.SequenceEqual(new[]
            { "ActiveDirectoryGroup1", "ActiveDirectoryGroup2" }));
    }

    [Fact]
    public void NoActiveDirectoryGroups_EmptySplittedActiveDirectoryGroups()
    {
        var settingsPath =
            TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}no-active-directory-groups.xml");
        var portalSetting = TestEnvironment.LoadPortalSettings(settingsPath);
        var adSettings = portalSetting.ActiveDirectorySettings;
        Assert.NotNull(adSettings);
        Assert.Empty(adSettings.SplittedActiveDirectoryGroups);
    }

    [Fact]
    public void ActiveDirectorySettingsConstructer_ShouldFillAllProperties()
    {
        var secondFactorGroup = "SecondFactorGroup1;SecondFactorGroup3;SecondFactorGroup3";
        var useUserPhone = true;
        var userMobileUserPhone = true;
        var netBiosName = "NetBiosName";
        var requiresUserPrincipalName = true;
        var activeDirectoryGroup = "ActiveDirectoryGroup1;ActiveDirectoryGroup2;ActiveDirectoryGroup3";
        var settings = new ActiveDirectorySettings(
            secondFactorGroup,
            useUserPhone,
            userMobileUserPhone,
            netBiosName,
            requiresUserPrincipalName,
            activeDirectoryGroup);

        Assert.NotNull(settings);
        Assert.True(new[] { "SecondFactorGroup1", "SecondFactorGroup3", "SecondFactorGroup3" }.SequenceEqual(settings.SecondFactorGroups));
        Assert.Equal(useUserPhone, settings.UseUserPhone);
        Assert.Equal(userMobileUserPhone, settings.UseMobileUserPhone);
        Assert.Equal(netBiosName, settings.NetBiosName);
        Assert.Equal(requiresUserPrincipalName, settings.RequiresUserPrincipalName);
        Assert.Equal(activeDirectoryGroup, settings.ActiveDirectoryGroup);
        var adGroups = new[] { "ActiveDirectoryGroup1", "ActiveDirectoryGroup2", "ActiveDirectoryGroup3" }.Order();
        Assert.True(adGroups.SequenceEqual(settings.SplittedActiveDirectoryGroups.Order()));
    }
}