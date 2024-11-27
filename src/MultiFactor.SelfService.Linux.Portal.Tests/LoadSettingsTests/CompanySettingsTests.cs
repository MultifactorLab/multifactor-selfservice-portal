using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

namespace MultiFactor.SelfService.Linux.Portal.Tests.LoadSettingsTests;

public class CompanySettingsTests
{
    [Fact]
    public void LoadAllCompanySettingsSettings()
    {
        var portalSettingsPath =
            TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}full-company-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(portalSettingsPath);
        var companySettings = portalSettings.CompanySettings;
        Assert.NotNull(companySettings);
        Assert.Equal("Name", companySettings.Name);
        Assert.Equal("Domain", companySettings.Domain);
        Assert.Equal("logo.svg", companySettings.LogoUrl);
        Assert.Equal("NestedGroupsBaseDn", companySettings.NestedGroupsBaseDn);
        Assert.True(companySettings.LoadActiveDirectoryNestedGroups);
    }

    [Fact]
    public void NoLoadActiveDirectoryNestedGroups_LoadActiveDirectoryNestedGroupsShouldBeFalse()
    {
        var portalSettingsPath =
            TestEnvironment.GetAssetPath(
                $"Settings{Path.DirectorySeparatorChar}no-load-active-directory-nested-groups.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(portalSettingsPath);
        var companySettings = portalSettings.CompanySettings;
        Assert.False(companySettings.LoadActiveDirectoryNestedGroups);
    }

    [Fact]
    public void NestedGroupsBaseDn_SplittedNestedGroupsDomainShouldNotEmpty()
    {
        var portalSettingsPath =
            TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}nested-groups-base-dn.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(portalSettingsPath);
        var companySettings = portalSettings.CompanySettings;
        Assert.False(string.IsNullOrWhiteSpace(companySettings.NestedGroupsBaseDn));
        var splittedNestedGroups = companySettings.SplittedNestedGroupsDomain;
        Assert.True(splittedNestedGroups.SequenceEqual(new string[] { "NestedGroupsBaseDn1", "NestedGroupsBaseDn2" }));
    }

    [Fact]
    public void CompanySettingsConstructor_ShouldFillAllProperties()
    {
        var name = "name";
        var domain = "domain";
        var logoUrl = "logo.svg";
        var nestedGroupsBaseDn = "NestedGroupsBaseDn1;NestedGroupsBaseDn2;NestedGroupsBaseDn3";
        var loadActiveDirectoryNestedGroups = true;
        var settings = new CompanySettings(
            name,
            domain,
            logoUrl,
            nestedGroupsBaseDn,
            loadActiveDirectoryNestedGroups);

        Assert.Equal(name, settings.Name);
        Assert.Equal(domain, settings.Domain);
        Assert.Equal(logoUrl, settings.LogoUrl);
        Assert.Equal(nestedGroupsBaseDn, settings.NestedGroupsBaseDn);
        Assert.Equal(loadActiveDirectoryNestedGroups, settings.LoadActiveDirectoryNestedGroups);
        var nestedGroups = new[] { "NestedGroupsBaseDn1", "NestedGroupsBaseDn2", "NestedGroupsBaseDn3" }.Order();
        Assert.True(nestedGroups.SequenceEqual(settings.SplittedNestedGroupsDomain.Order()));
    }
}