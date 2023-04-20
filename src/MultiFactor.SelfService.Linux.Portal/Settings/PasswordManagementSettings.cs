namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class PasswordManagementSettings
    {
        public bool Enabled { get; init; }
        public bool AllowPasswordRecovery { get; init; }
        public ChangePasswordMode ChangeValidPasswordMode { get; init; } =
            ChangePasswordMode.AsUser;
        public ChangePasswordMode ChangeExpiredPasswordMode { get; init; } =
            ChangePasswordMode.AsTechnicalAccount;
        public TimeSpan? PasswordChangingSessionLifetime { get; init; }
        public long? PasswordChangingSessionCacheSize { get; init; }
    }

    [Obsolete]
    public class PasswordChangingSessionSettingsObsolete
    {
        [Obsolete]
        public TimeSpan? PwdChangingSessionLifetime { get; set; }
        [Obsolete]
        public long? PwdChangingSessionCacheSize { get; set; }
    }
}
