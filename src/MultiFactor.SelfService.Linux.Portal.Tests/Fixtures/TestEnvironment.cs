using Microsoft.AspNetCore.Builder;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Tests.Fixtures;

internal static class TestEnvironment
{
    private static readonly string AppFolder = $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)}{Path.DirectorySeparatorChar}";
    private static readonly string AssetsFolder = $"{AppFolder}Assets";
    
    public static string GetAssetPath(string fileName)
    {
        return string.IsNullOrWhiteSpace(fileName) ? AssetsFolder : $"{AssetsFolder}{Path.DirectorySeparatorChar}{fileName}";
    }
    
    public static PortalSettings LoadPortalSettings(string settingsPath)
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddConfiguration(settingsPath).RegisterConfiguration();
        var app = builder.Build();
        var portalSettings = (PortalSettings)app.Services.GetService(typeof(PortalSettings))!;
        return portalSettings;
    }
}