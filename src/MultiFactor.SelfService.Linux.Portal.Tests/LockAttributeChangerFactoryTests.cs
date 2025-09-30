
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

namespace MultiFactor.SelfService.Linux.Portal.Tests;

public class LockAttributeChangerFactoryTests
{
    [Theory]
    [InlineData(LdapImplementation.OpenLdap)]
    [InlineData(LdapImplementation.ActiveDirectory)]
    [InlineData(LdapImplementation.FreeIPA)]
    public void GetChanger_ShouldReturnRightChanger(LdapImplementation expectedLdapImplementation)
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
        var filterProvider = SetupMockLdapProfileFilterProvider();
        var builder = GetAppBuilder(portalSettings);
        builder.Services.ReplaceService(filterProvider);
        var app = builder.Build();
        var attributeChangerMock = new Mock<IUserAttributeChanger>();
        var loggerMock = new Mock<ILogger<ILockAttributeChanger>>();
        var profileLoader = (LdapProfileLoader)app.Services.GetService<LdapProfileLoader>();
        var connectionFactory = (LdapConnectionAdapterFactory)app.Services.GetService<LdapConnectionAdapterFactory>();;
        var serverInfo = new LdapServerInfo(expectedLdapImplementation);
        
        var factory = new LockAttributeChangerFactory(serverInfo, attributeChangerMock.Object, connectionFactory, profileLoader, loggerMock.Object);
        var changer = factory.CreateChanger();
        
        Assert.NotNull(changer);
        Assert.NotNull(changer.AttributeName);
        var typeName = changer.GetType().Name;
        var actualLdapImplementation = GetChangerTypeByAttributeName(typeName);
        Assert.Equal(expectedLdapImplementation, actualLdapImplementation);
    }

    private LdapImplementation GetChangerTypeByAttributeName(string typeName) => typeName switch
    {
        "AdLockAttributeChanger" => LdapImplementation.ActiveDirectory,
        "OpenLdapLockAttributeChanger" => LdapImplementation.OpenLdap,
        "IpaLockAttributeChanger" => LdapImplementation.FreeIPA,
        _ => throw new ArgumentException($"Unknown type: {typeName}")
    };
    
    private ILdapProfileFilterProvider SetupMockLdapProfileFilterProvider()
    {
        var filterProvider = new Mock<ILdapProfileFilterProvider>();
        filterProvider
            .Setup(x => x.GetProfileSearchFilter(It.IsAny<LdapIdentity>()))
            .Returns(LdapFilter.Create("objectClass", "user", "person", "memberof"));
        return filterProvider.Object;
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
            .AddSingleton<IBindIdentityFormatter, DefaultBindIdentityFormatter>()
            .AddSingleton<LdapProfileLoader>()
            .AddSingleton<LdapConnectionAdapterFactory>()
            .AddSingleton<IMemoryCache, MemoryCache>();
        
        return builder;
    }
}