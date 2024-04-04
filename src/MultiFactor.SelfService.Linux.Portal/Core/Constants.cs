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

        public const string CredentialVerificationResult = "CredentialVerificationResult";
        public const string SsoClaims = "SsoClaims";
        public const string LoadedLdapAttributes = "LoadedLdapAttributes";

        public static readonly string WORKING_DIRECTORY = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        public static readonly string LOG_DIRECTORY = $"{WORKING_DIRECTORY}/logs";
        public static readonly string KEY_STORAGE_DIRECTORY = $"{WORKING_DIRECTORY}/key-storage";
        public const string PWD_RECOVERY_COOKIE = "PSession";
        public const string PWD_RENEWAL_PURPOSE = "PwdRenewal";
        /// <summary>
        /// Group 1; группа 2 ; ГРУППА_3;
        /// </summary>
        public const string SIGN_UP_GROUPS_REGEX = @"([\wа-я\s\-]+)(\s*;\s*([\wа-я\s\-]+)*)*";
        public const long BYTES_IN_MB = 1048576L;
        public static class MultiFactorClaims
        {
            public const string SamlSessionId = "samlSessionId";
            public const string OidcSessionId = "oidcSessionId";
            public const string ChangePassword = "changePassword";
            public const string ResetPassword = "resetPassword";
            public const string RawUserName = "rawUserName";
        }
    }
}