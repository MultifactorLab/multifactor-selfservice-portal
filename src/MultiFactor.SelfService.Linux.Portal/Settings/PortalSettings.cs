using MultiFactor.SelfService.Linux.Portal.Core.Caching;

namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class PortalSettings
    {
        public const string SectionName = "PortalSettings";

        public CompanySettings CompanySettings { get; private set; } = new();
        public TechnicalAccountSettings TechnicalAccountSettings { get; private set; } = new();
        public ActiveDirectorySettings ActiveDirectorySettings { get; private set; } = new();
        public MultiFactorApiSettings MultiFactorApiSettings { get; private set; } = new();
        public GroupPolicyPreset GroupPolicyPreset { get; private set; } = new();
        public CaptchaSettings CaptchaSettings { get; set; } = new();
        public PasswordManagementSettings? PasswordManagement { get; set; }
        public bool RequiresUserPrincipalName { get; private set; } 
        public string? LoggingLevel { get; private set; }
        public string? LoggingFormat { get; private set; }
        public string? SyslogFormat { get; private set; }
        public string? SyslogFacility { get; private set; }
        public string? SyslogAppName { get; private set; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; private set; }
        public string UICulture { get; private set; } = string.Empty;
        public string LdapBaseDn { get; private set; } = string.Empty;


        [Obsolete("Use PasswordChangingManagementSettings.EnablePassword property instead")]
        public bool EnablePasswordManagement { get; private set; }
        [Obsolete("Use PasswordChangingManagementSettings.ChangeValidPasswordMode property instead")]
        public ChangePasswordMode ChangeValidPasswordMode { get; private set; } =
            ChangePasswordMode.AsUser;
        [Obsolete("Use PasswordChangingManagementSettings.ChangeExpiredPasswordMode property instead")]
        public ChangePasswordMode ChangeExpiredPasswordMode { get; private set; } =
            ChangePasswordMode.AsTechnicalAccount;
        [Obsolete("Use PasswordChangingManagementSettings instead")]
        public PasswordChangingSessionSettings PasswordChangingSessionSettings { get; private set; } = new();
        [Obsolete("Use CaptchaSettings property instead")]
        public GoogleReCaptchaSettings GoogleReCaptchaSettings { get; private set; } = new();
    }

    public enum ChangePasswordMode
    {
        AsUser,
        AsTechnicalAccount
    }
}
