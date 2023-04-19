namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class PasswordManagementSettings
    {
        public bool Enabled { get; private set; }
        public bool AllowPasswordRecovery { get; private set; }
        public ChangePasswordMode ChangeValidPasswordMode { get; private set; } =
            ChangePasswordMode.AsUser;
        public ChangePasswordMode ChangeExpiredPasswordMode { get; private set; } =
            ChangePasswordMode.AsTechnicalAccount;
        public TimeSpan? PasswordChangingSessionLifetime { get; set; }
        public long? PasswordChangingSessionCacheSize { get; set; }

        public PasswordManagementSettings() { }
        public PasswordManagementSettings(
            bool isPasswordManagementEnabled,
            bool isPasswordRecoveryEnabled,
            ChangePasswordMode changeValidPwdMode,
            ChangePasswordMode changeExpiredPwdMode,
            TimeSpan? pwdChangingSessionLifetime,
            long? pwdChangingSessionCacheSize)
        {
            Enabled = isPasswordManagementEnabled;
            AllowPasswordRecovery = isPasswordRecoveryEnabled;
            ChangeValidPasswordMode = changeValidPwdMode;
            ChangeExpiredPasswordMode = changeExpiredPwdMode;
            PasswordChangingSessionLifetime = pwdChangingSessionLifetime;
            PasswordChangingSessionCacheSize = pwdChangingSessionCacheSize;
        }
    }

    [Obsolete]
    public class PasswordChangingSessionSettingsObsolete
    {
        [Obsolete]
        public TimeSpan? PwdChangingSessionLifetime { get; set; }
        [Obsolete]
        public long? PwdChangingSessionCacheSize { get; set; }
    }

    public class PasswordChangingSessionSettings
    {
        public TimeSpan? Lifetime { get; set; }
        public long? CacheSize { get; set; }
    }
}
