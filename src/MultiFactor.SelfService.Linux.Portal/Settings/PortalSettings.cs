namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class PortalSettings
    {
        public const string SectionName = "PortalSettings";

        public CompanySettings CompanySettings { get; private set; } = new();
        public TechnicalAccountSettings TechnicalAccountSettings { get; private set; } = new();
        public ActiveDirectorySettings ActiveDirectorySettings { get; private set; } = new();
        public MultiFactorApiSettings MultiFactorApiSettings { get; private set; } = new();
        public GoogleReCaptchaSettings GoogleReCaptchaSettings { get; private set; } = new();

        public bool RequiresUserPrincipalName { get; private set; }
        public string LoggingLevel { get; private set; }
        public string LoggingFormat { get; private set; }
        public string SyslogFormat { get; private set; }
        public string SyslogFacility { get; private set; }
        public string SyslogAppName { get; private set; }
        public bool EnablePasswordManagement { get; private set; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; private set; }
        public string UICulture { get; private set; }
    }
}
