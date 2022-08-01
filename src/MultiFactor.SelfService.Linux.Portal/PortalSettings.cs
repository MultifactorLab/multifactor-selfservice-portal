namespace MultiFactor.SelfService.Linux.Portal
{
    public class PortalSettings
    {
        public const string SectionName = "PortalSettings";

        /// <summary>
        /// Company name.
        /// </summary>
        public string CompanyName { get; private set; }

        /// <summary>
        /// Active Directory Domain.
        /// </summary>
        public string CompanyDomain { get; private set; }

        /// <summary>
        /// Active Directory technical user.
        /// </summary>
        public string TechnicalAccUsr { get; private set; }

        /// <summary>
        /// Active Directory technical user password.
        /// </summary>
        public string TechnicalAccPwd { get; private set; }

        /// <summary>
        /// Company Logo URL.
        /// </summary>
        public string CompanyLogoUrl { get; private set; }

        /// <summary>
        /// Only members of this group required to pass 2fa to access (Optional).
        /// </summary>
        public string ActiveDirectory2FaGroup { get; private set; }

        /// <summary>
        /// Use ActiveDirectory User general properties phone number (Optional).
        /// </summary>
        public bool UseActiveDirectoryUserPhone { get; private set; }

        /// <summary>
        /// Use ActiveDirectory User general properties mobile phone number (Optional).
        /// </summary>
        public bool UseActiveDirectoryMobileUserPhone { get; private set; }

        /// <summary>
        /// Active Directory NetBIOS Name to add to login.
        /// </summary>
        public string NetBiosName { get; private set; }

        /// <summary>
        /// Only UPN user name format permitted.
        /// </summary>
        public bool RequiresUserPrincipalName { get; private set; }

        /// <summary>
        /// Multifactor API URL.
        /// </summary>
        public string MultiFactorApiUrl { get; private set; }

        /// <summary>
        /// HTTP Proxy for API.
        /// </summary>
        public string MultiFactorApiProxy { get; private set; }

        /// <summary>
        /// Multifactor API Key.
        /// </summary>
        public string MultiFactorApiKey { get; private set; }

        /// <summary>
        /// Multifactor API Secret.
        /// </summary>
        public string MultiFactorApiSecret { get; private set; }

        /// <summary>
        /// Logging level.
        /// </summary>
        public string LoggingLevel { get; private set; }

        public string LoggingFormat { get; private set; }

        public string SyslogFormat { get; private set; }

        public string SyslogFacility { get; private set; }

        public string SyslogAppName { get; private set; }

        public bool EnablePasswordManagement { get; private set; }

        public bool EnableExchangeActiveSyncDevicesManagement { get; private set; }

        /// <summary>
        /// Sets application culture.
        /// </summary>
        public string UICulture { get; private set; }
    }
}
