using MultiFactor.SelfService.Linux.Portal.Settings;
using Serilog;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static partial class ConfigurationRegistering
    {
        /// <summary>
        /// Loads configuration and adds it as a singletone in the DI container.
        /// Must be called after the logger is configered (<see cref="Logging.ConfigureLogging(WebApplicationBuilder)"/>).
        /// </summary>
        public static WebApplicationBuilder RegisterConfiguration(this WebApplicationBuilder applicationBuilder)
        {
            try
            {
                var settings = GetSettings(applicationBuilder.Configuration) ??
                               throw new Exception("Can't find PortalSettings section in appsettings");
                MapObsoleteSections(settings);
                Validate(settings);

                applicationBuilder.Services.AddSingleton(settings);
                return applicationBuilder;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Unable to start");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void MapObsoleteSections(PortalSettings settings)
        {
            if (settings.GoogleReCaptchaSettings.Enabled && !settings.CaptchaSettings.Enabled)
            {
                settings.CaptchaSettings = new CaptchaSettings()
                {
                    Enabled = settings.GoogleReCaptchaSettings.Enabled,
                    Key = settings.GoogleReCaptchaSettings.Key,
                    Secret = settings.GoogleReCaptchaSettings.Secret,
                    CaptchaType = CaptchaType.Google
                };
            }

            if (settings.PasswordManagement == null)
            {
                settings.PasswordManagement = new PasswordManagementSettings()
                {
                    Enabled = settings.EnablePasswordManagement,
                    AllowPasswordRecovery = false,
                    ChangeValidPasswordMode = settings.ChangeValidPasswordMode,
                    ChangeExpiredPasswordMode = settings.ChangeExpiredPasswordMode,
                    PasswordChangingSessionLifetime =
                        settings.PasswordChangingSessionSettings?.PwdChangingSessionLifetime,
                    PasswordChangingSessionCacheSize =
                        settings.PasswordChangingSessionSettings?.PwdChangingSessionCacheSize
                };
            }

            if (settings.ExchangeActiveSyncDevicesManagement == null ||
                settings.EnableExchangeActiveSyncDevicesManagement)
            {
                settings.ExchangeActiveSyncDevicesManagement = new ExchangeActiveSyncDevicesManagement
                {
                    Enabled = settings.EnableExchangeActiveSyncDevicesManagement
                };
            }

            if (settings.RequiresUserPrincipalName)
            {
                settings.ActiveDirectorySettings = new ActiveDirectorySettings(
                    // dirt so as not to break the encapsulation and not to write a separate mapper
                    string.Join(';', settings.ActiveDirectorySettings.SecondFactorGroups),
                    settings.ActiveDirectorySettings.UseUserPhone,
                    settings.ActiveDirectorySettings.UseMobileUserPhone,
                    settings.ActiveDirectorySettings.NetBiosName,
                    settings.RequiresUserPrincipalName,
                    settings.ActiveDirectorySettings.ActiveDirectoryGroup);
            }
        }

        private static PortalSettings GetSettings(IConfigurationRoot config)
        {
            var section = config.GetSection(nameof(PortalSettings));
            return section.Get<PortalSettings>(o => { o.BindNonPublicProperties = true; });
        }

        private static void Validate(PortalSettings settings)
        {
            var result = new PortalSettingsValidator().Validate(settings);
            if (result.IsValid) return;

            var aggregatedMsg = result.Errors.Select(x => x.ErrorMessage)
                .Aggregate((acc, cur) => $"{acc}{Environment.NewLine}{cur}");
            throw new Exception($"Configuration errors: {Environment.NewLine}{aggregatedMsg}");
        }
    }
}