namespace MultiFactor.SelfService.Linux.Portal.Core
{
    public class Constants
    {
        public const string COOKIE_NAME = "multifactor";
        public const string SESSION_EXPIRED_PASSWORD_USER_KEY = "multifactor:expired-password:user";
        public const string SESSION_EXPIRED_PASSWORD_CIPHER_KEY = "multifactor:expired-password:cipher";
        public const string TOKEN_VALIDATION = "TokenValidation:JsonWebKeySet";
        public const string ENVIRONMENT_KEY = "Environment";
        public const string PRODUCTION_ENV = "production";

        public static class MultiFactorClaims
        {
            public const string SamlSessionId = "samlSessionId";
            public const string OidcSessionId = "oidcSessionId";
            public const string ChangePassword = "changePassword";
            public const string RawUserName = "rawUserName";
        }
    }
}