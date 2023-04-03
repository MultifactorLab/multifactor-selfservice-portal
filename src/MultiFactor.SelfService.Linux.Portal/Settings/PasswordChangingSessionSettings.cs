namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class PasswordChangingSessionSettings
    {
        public TimeSpan? PwdChangingSessionLifetime { get; private set; }
        public long? PwdChangingSessionCacheSize { get; private set; }
    }
}
