using LdapForNet;
using LdapForNet.Native;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

namespace MultiFactor.SelfService.Linux.Portal.Tests;

public class LdapProfileLoaderTests
{
    [Fact]
    public async Task LoadProfile_LoadNestedGroupsIsFalse_ShouldLoadOnlyGroupsFromMemberOf()
    {
        var portalSettings = new PortalSettings(
            new CompanySettings(
                "Name",
                "Domain",
                "logo",
                string.Empty,
                loadActiveDirectoryNestedGroups: false
            ),
            new ActiveDirectorySettings());
        
        var nestedGroupsDns = Array.Empty<string>();
        var domainName = "domain";
        var ldapDomain = LdapDomain.Parse(domainName);
        var domainAttributes = new SearchResultAttributeCollection();
        var memberOf = new DirectoryAttribute() { Name = "memberOf", };
        memberOf.Add("g=group,d=domain");
        domainAttributes.Add(memberOf);
        var domainEntry = new LdapEntry() { Dn = domainName, DirectoryAttributes = domainAttributes };
        var mockedConnection = SetupMockLdapConnectionAdapter(nestedGroupsDns, domainEntry);
        var filterProvider = SetupMockLdapProfileFilterProvider();
        var builder = GetAppBuilder(portalSettings);
        builder.Services.ReplaceService(filterProvider);
        var app = builder.Build();
        
        var loader = (LdapProfileLoader)app.Services.GetService<LdapProfileLoader>();
        var profile = await loader.LoadProfileAsync(ldapDomain, new LdapIdentity(domainName, IdentityType.DistinguishedName), mockedConnection);
        var loadedGroups = profile.Attributes.Entries.FirstOrDefault(x => x.Name == "memberOf")?.Values;
        
        Assert.NotNull(loadedGroups);
        Assert.Single(loadedGroups);
        Assert.True(loadedGroups!.Order().SequenceEqual(memberOf.GetValues<string>().Select(LdapIdentity.DnToCn).Order()));
    }
    
    [Fact]
    public async Task LoadProfile_LoadNestedGroupsIsTrue_ShouldLoadMemberOfGroupsAndNestedGroups()
    {
        var nestedGroupsDns = new string [] { "DC=nestedGroup1,a", "DC=nestedGroup2,a", "DC=nestedGroup3,a" };
        var portalSettings = new PortalSettings(
            new CompanySettings(
                "Name",
                "Domain",
                "logo",
                string.Join(";", nestedGroupsDns),
                loadActiveDirectoryNestedGroups: true
            ),
            new ActiveDirectorySettings());
        
        var domain = "domain";
        var ldapDomain = LdapDomain.Parse(domain);
        var domainAttributes = new SearchResultAttributeCollection();
        var memberOf = new DirectoryAttribute() { Name = "memberOf", };
        memberOf.Add("g=group,d=domain");
        domainAttributes.Add(memberOf);
        var memberOfGroups = memberOf.GetValues<string>().Select(LdapIdentity.DnToCn);
        var mergedTestGroups = memberOfGroups.Concat(nestedGroupsDns.Select(LdapIdentity.DnToCn));
        var domainEntry = new LdapEntry() { Dn = domain, DirectoryAttributes = domainAttributes };
        var mockedConnection = SetupMockLdapConnectionAdapter(portalSettings.CompanySettings.SplittedNestedGroupsDomain, domainEntry);
        var filterProvider = SetupMockLdapProfileFilterProvider();
        var builder = GetAppBuilder(portalSettings);
        builder.Services.ReplaceService(filterProvider);
        var app = builder.Build();
        
        var loader = (LdapProfileLoader)app.Services.GetService<LdapProfileLoader>();
        var profile = await loader.LoadProfileAsync(ldapDomain, new LdapIdentity(domain, IdentityType.DistinguishedName), mockedConnection);
        var groups = profile.Attributes.Entries.FirstOrDefault(x => x.Name == "memberOf")?.Values;
        
        Assert.NotNull(groups);
        Assert.True(groups!.Order().SequenceEqual(mergedTestGroups.Order()));
    }

    [Fact]
    public async Task LoadProfile_NoNestedGroups_ShouldLoadGroupsFromDomain()
    {
        var portalSettings = new PortalSettings(
            new CompanySettings(
                "Name",
                "Domain",
                "logo",
                nestedGroupsBaseDn: string.Empty,
                loadActiveDirectoryNestedGroups: true
            ),
            new ActiveDirectorySettings());
        
        var domain = "g=group,d=domain";
        var ldapDomain = LdapDomain.Parse(domain);
        var domainAttributes = new SearchResultAttributeCollection();
        var memberOf = new DirectoryAttribute() { Name = "memberOf", };
        memberOf.Add(domain);
        domainAttributes.Add(memberOf);
        var domainEntry = new LdapEntry() { Dn = domain, DirectoryAttributes = domainAttributes };
        var mockedConnection = SetupMockLdapConnectionAdapter(memberOf.GetValues<string>(), domainEntry);
        var filterProvider = SetupMockLdapProfileFilterProvider();
        var builder = GetAppBuilder(portalSettings);
        builder.Services.ReplaceService(filterProvider);
        var app = builder.Build();
        
        var loader = (LdapProfileLoader)app.Services.GetService<LdapProfileLoader>();
        var profile = await loader.LoadProfileAsync(ldapDomain, new LdapIdentity(domain, IdentityType.DistinguishedName), mockedConnection);
        var groups = profile.Attributes.Entries.FirstOrDefault(x => x.Name == "memberOf")?.Values;
        
        Assert.NotNull(groups);
        Assert.True(groups!.Order().SequenceEqual(memberOf.GetValues<string>().Select(LdapIdentity.DnToCn).Order()));
    }

    [Fact]
    public async Task LoadProfile_NoMemberOfAttribute_ShouldLoadOnlyNestedGroups()
    {
        var nestedGroupsDns = new string [] { "DC=nestedGroup1,a", "DC=nestedGroup2,a", "DC=nestedGroup3,a" };
        var portalSettings = new PortalSettings(
            new CompanySettings(
                "Name",
                "Domain",
                "logo",
                string.Join(";", nestedGroupsDns),
                loadActiveDirectoryNestedGroups: true
                ),
            new ActiveDirectorySettings());
        
        var builder = GetAppBuilder(portalSettings);
        var domain = "domain";
        var ldapDomain = LdapDomain.Parse(domain);
        var domainEntry = new LdapEntry() { Dn = domain, DirectoryAttributes = new SearchResultAttributeCollection() };
        var mockedConnection = SetupMockLdapConnectionAdapter(nestedGroupsDns, domainEntry);
        var filterProvider = SetupMockLdapProfileFilterProvider();
        builder.Services.ReplaceService(filterProvider);
        var app = builder.Build();
        
        var loader = (LdapProfileLoader)app.Services.GetService<LdapProfileLoader>();
        var profile = await loader.LoadProfileAsync(ldapDomain, new LdapIdentity(domain, IdentityType.DistinguishedName), mockedConnection);
        var groups = profile.Attributes.Entries.FirstOrDefault(x => x.Name == "memberOf")?.Values;
        
        Assert.NotNull(groups);
        Assert.NotEmpty(groups);
        Assert.True(groups!.Order().SequenceEqual(portalSettings.CompanySettings.SplittedNestedGroupsDomain.Select(LdapIdentity.DnToCn).Order()));
    }

    private ILdapProfileFilterProvider SetupMockLdapProfileFilterProvider()
    {
        var filterProvider = new Mock<ILdapProfileFilterProvider>();
        filterProvider
            .Setup(x => x.GetProfileSearchFilter(It.IsAny<LdapIdentity>()))
            .Returns(LdapFilter.Create("objectClass", "user", "person", "memberof"));
        return filterProvider.Object;
    }

    private ILdapConnectionAdapter SetupMockLdapConnectionAdapter(IEnumerable<string> nestedGroups, LdapEntry domainEntry)
    {
        var mockConnection = new Mock<ILdapConnectionAdapter>();
        mockConnection.Setup(x => x.SearchQueryAsync(
                domainEntry.Dn,
                It.IsAny<string>(),
                Native.LdapSearchScope.LDAP_SCOPE_SUB,
                It.IsAny<string[]>()))
            .ReturnsAsync(new List<LdapEntry>() { domainEntry });
        
        foreach (var group in nestedGroups)
        {
            mockConnection
                .Setup(x => x.SearchQueryAsync(
                    group,
                    It.IsAny<string>(),
                    It.IsAny<Native.LdapSearchScope>(),
                   "DistinguishedName"))
                .ReturnsAsync(new List<LdapEntry>() { new LdapEntry() {Dn = group}});
        }

        return mockConnection.Object;
    }

    private WebApplicationBuilder GetAppBuilder(PortalSettings portalSettings)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services
            .AddSingleton(portalSettings)
            .AddSingleton<AdditionalClaimDescriptorsProvider>()
            .AddSingleton<AdditionalClaimsMetadata>()
            .AddSingleton<ILdapConnectionAdapter, LdapConnectionAdapter>()
            .AddSingleton<ILdapProfileFilterProvider, LdapProfileFilterProvider>()
            .AddSingleton<LdapProfileLoader>();
        
        return builder;
    }
}