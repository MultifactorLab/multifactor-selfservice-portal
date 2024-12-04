using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

namespace MultiFactor.SelfService.Linux.Portal.Tests.LoadSettingsTests;

public class LoadDefaultSettingsTests
{
    [Fact]
    public void LoadDefaultSettings_ShouldLoadPortalSettings()
    {
        var configPath = TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(configPath);
        Assert.NotNull(portalSettings);
    }

    [Fact]
    public void LoadDefaultSettings_ShouldLoadCompanySettings()
    {  
        var configPath = TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(configPath);
        var companySettings = portalSettings.CompanySettings;
        
        Assert.NotNull(companySettings);
        Assert.Equal("ACME", companySettings!.Name);
        Assert.Equal("domain.local", companySettings.Domain);
        Assert.Equal("logo.svg", companySettings.LogoUrl);
    }

    [Fact]
    public void LoadDefaultSettings_ShouldLoadTechnicalAccountSettings()
    {
        var configPath = TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(configPath);
        var technicalAccountSettings = portalSettings.TechnicalAccountSettings;
        
        Assert.NotNull(technicalAccountSettings);
        Assert.Equal("user", technicalAccountSettings.User);
        Assert.Equal("password", technicalAccountSettings.Password);
    }

    [Fact]
    public void LoadDefaultSettings_ShouldLoadActiveDirectorySettings()
    {
        var configPath = TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(configPath);
        var activeDirectorySettings = portalSettings.ActiveDirectorySettings;
        
        Assert.NotNull(activeDirectorySettings);
        Assert.False(activeDirectorySettings.RequiresUserPrincipalName);
        Assert.False(activeDirectorySettings.UseUpnAsIdentity);
    }
    
    [Fact]
    public void LoadDefaultSettings_ShouldLoadCaptchaSettings()
    {
        var configPath = TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(configPath);
        var captchaSettings = portalSettings.CaptchaSettings;
        
        Assert.NotNull(captchaSettings);
        Assert.Equal(CaptchaType.Yandex, captchaSettings.CaptchaType);
        Assert.Equal("key", captchaSettings.Key);
        Assert.Equal("secret", captchaSettings.Secret);
        Assert.Equal(CaptchaRequired.Always, captchaSettings.CaptchaRequired);
    }
    
    [Fact]
    public void LoadDefaultSettings_ShouldLoadMultifactorApiSettings()
    {
        var configPath = TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(configPath);
        var mfSettings = portalSettings.MultiFactorApiSettings;
        
        Assert.NotNull(mfSettings);
        Assert.Equal("ApiUrl", mfSettings.ApiUrl);
        Assert.Equal("ApiKey", mfSettings.ApiKey);
        Assert.Equal("Secret", mfSettings.ApiSecret);
    }
    
    [Fact]
    public void LoadDefaultSettings_ShouldLoadPasswordManagementSettings()
    {
        var configPath = TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(configPath);
        var passwordManagementSettings = portalSettings.PasswordManagement;
        
        Assert.NotNull(passwordManagementSettings);
        Assert.False(passwordManagementSettings.Enabled);
        Assert.False(passwordManagementSettings.AllowPasswordRecovery);
    }
    
    [Fact]
    public void LoadDefaultSettings_ShouldLoadExchangeActiveSyncDevicesManagementSettings()
    {
        var configPath = TestEnvironment.GetAssetPath($"Settings{Path.DirectorySeparatorChar}minimal-settings.xml");
        var portalSettings = TestEnvironment.LoadPortalSettings(configPath);
        var exchangeActiveSyncDevicesManagementSettings = portalSettings.ExchangeActiveSyncDevicesManagement;
        
        Assert.NotNull(exchangeActiveSyncDevicesManagementSettings);
        Assert.False(exchangeActiveSyncDevicesManagementSettings.Enabled);
    }
}