namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class PasswordManagementSettings
    {
        public bool PasswordManagementEnabled { get; private init; }
        public bool PasswordRecoveryEnabled { get; private init; }
        public ChangePasswordMode ChangeValidPasswordMode { get; private init; } =
            ChangePasswordMode.AsUser;
        public ChangePasswordMode ChangeExpiredPasswordMode { get; private init; } =
            ChangePasswordMode.AsTechnicalAccount;
        public TimeSpan? PwdChangingSessionLifetime { get; private init; }
        public long? PwdChangingSessionCacheSize { get; private init; }

        public PasswordManagementSettings() { }
        public PasswordManagementSettings(
            bool isPasswordManagementEnabled,
            bool isPasswordRecoveryEnabled,
            ChangePasswordMode changeValidPwdMode,
            ChangePasswordMode changeExpiredPwdMode,
            TimeSpan? pwdChangingSessionLifetime,
            long? pwdChangingSessionCacheSize)
        {
            PasswordManagementEnabled = isPasswordManagementEnabled;
            PasswordRecoveryEnabled = isPasswordRecoveryEnabled;
            ChangeValidPasswordMode = changeValidPwdMode;
            ChangeExpiredPasswordMode = changeExpiredPwdMode;
            PwdChangingSessionLifetime = pwdChangingSessionLifetime;
            PwdChangingSessionCacheSize = pwdChangingSessionCacheSize;
        }
    }


    public class PasswordChangingSessionSettings
    {
        public TimeSpan? PwdChangingSessionLifetime { get; private set; }
        public long? PwdChangingSessionCacheSize { get; private set; }
    }
}
