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
        public const string CAPTCHA_TOKEN = "responseToken";

        public const string WORKING_DIRECTORY = "/opt/multifactor/ssp";
        public const string LOG_DIRECTORY = $"{WORKING_DIRECTORY}/logs";
        public const string KEY_STORAGE_DIRECTORY = $"{WORKING_DIRECTORY}/key-storage";

        public static class MultiFactorClaims
        {
            public const string SamlSessionId = "samlSessionId";
            public const string OidcSessionId = "oidcSessionId";
            public const string ChangePassword = "changePassword";
            public const string RawUserName = "rawUserName";
        }
    }
}