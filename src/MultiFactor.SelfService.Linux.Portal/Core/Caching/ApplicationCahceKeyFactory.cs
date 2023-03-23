namespace MultiFactor.SelfService.Linux.Portal.Core.Caching
{
    public static class ApplicationCacheKeyFactory
    {
        public static string CreateExpiredPwdUserKey(string identity)
        {
            return $"{Constants.SESSION_EXPIRED_PASSWORD_USER_KEY}:{identity}";
        }

        public static string CreateExpiredPwdCipherKey(string identity)
        {
            return $"{Constants.SESSION_EXPIRED_PASSWORD_CIPHER_KEY}:{identity}";
        }
    }
}
