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
        public TimeSpan? PwdChangingSessionLifetime { get; private set; }
        public long? PwdChangingSessionCacheSize { get; private set; }

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
            PwdChangingSessionLifetime = pwdChangingSessionLifetime;
            PwdChangingSessionCacheSize = pwdChangingSessionCacheSize;
        }
    }


    public class PasswordChangingSessionSettings
    {
        public TimeSpan? PwdChangingSessionLifetime { get; set; }
        public long? PwdChangingSessionCacheSize { get; set; }
    }
}
