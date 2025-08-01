﻿using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core;

namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class PortalSettings
    {
        public const string SectionName = "PortalSettings";

        public CompanySettings CompanySettings { get; private set; } = new();
        public TechnicalAccountSettings TechnicalAccountSettings { get; private set; } = new();
        public ActiveDirectorySettings ActiveDirectorySettings { get; set; } = new();
        public MultiFactorApiSettings MultiFactorApiSettings { get; private set; } = new();
        public MultifactorIdpApiSettings MultifactorIdpApiSettings { get; private set; } = new();
        public GroupPolicyPreset GroupPolicyPreset { get; private set; } = new();
        public AdditionalClaims AdditionalClaims { get; private set; } = new();
        public CaptchaSettings CaptchaSettings { get; set; } = new();
        public PasswordManagementSettings PasswordManagement { get; set; }
        public ShowcaseLinks LinksShowcase { get; set; } = new();
        public ExchangeActiveSyncDevicesManagement ExchangeActiveSyncDevicesManagement { get; set; }
        public PasswordRequirementsSection PasswordRequirements { get; init; } = new();
        public string LoggingLevel { get; private set; }
        public string LoggingFormat { get; private set; }
        public string UICulture { get; private set; } = string.Empty;
        public string LdapBaseDn { get; private set; } = string.Empty;
        public bool PreAuthenticationMethod { get; private set; }

        public PortalSettings(){}

        public PortalSettings(CompanySettings companySettings, ActiveDirectorySettings activeDirectorySettings)
        {
            CompanySettings = companySettings;
            ActiveDirectorySettings = activeDirectorySettings;
        }

        /// <summary>
        /// The legitimate user's bind may fail (e.g. expired password).
        /// However, sometimes we STILL need to get some information from the LDAP-directory.
        /// So we'll get it from under the technical account
        /// </summary>
        /// <returns></returns>
        public bool NeedPrebindInfo()
        {
            return
                ActiveDirectorySettings.UseUpnAsIdentity
                || !string.IsNullOrWhiteSpace(ActiveDirectorySettings.UseAttributeAsIdentity)
                || ActiveDirectorySettings.SecondFactorGroups.Length != 0
                || ActiveDirectorySettings.SplittedActiveDirectoryGroups.Length != 0
                || PasswordManagement.Enabled;
        }

        [Obsolete("Use ExchangeActiveSyncDevicesManagement.Enable instead")]
        public bool EnableExchangeActiveSyncDevicesManagement { get; private set; }

        [Obsolete("Use PasswordChangingManagementSettings.Enable property instead")]
        public bool EnablePasswordManagement { get; private set; }

        [Obsolete("Use PasswordChangingManagementSettings.ChangeValidPasswordMode property instead")]
        public ChangePasswordMode ChangeValidPasswordMode { get; private set; } =
            ChangePasswordMode.AsUser;

        [Obsolete("Use PasswordChangingManagementSettings.ChangeExpiredPasswordMode property instead")]
        public ChangePasswordMode ChangeExpiredPasswordMode { get; private set; } =
            ChangePasswordMode.AsTechnicalAccount;

        [Obsolete("Use PasswordChangingManagementSettings instead")]
        public PasswordChangingSessionSettingsObsolete PasswordChangingSessionSettings { get; private set; } = new();

        [Obsolete("Use CaptchaSettings property instead")]
        public GoogleReCaptchaSettings GoogleReCaptchaSettings { get; private set; } = new();

        [Obsolete("Use ActiveDirectorySettings.RequiresUserPrincipalName")]
        public bool RequiresUserPrincipalName { get; private set; }
    }

    public enum ChangePasswordMode
    {
        AsUser,
        AsTechnicalAccount
    }
}
