using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification
{
    public interface ICredentialVerificationResult
    {
        bool IsAuthenticated { get; }
        string? Reason { get; }

        bool IsBypass { get; }
        bool UserMustChangePassword { get; }

        string? DisplayName { get; }
        string? Email { get; }
        string? Phone { get; }
        string? Username { get; }
    }

    public class CredentialVerificationResult : ICredentialVerificationResult
    {
        public bool IsAuthenticated { get; }
        public string? Reason { get; private set; }

        public bool IsBypass { get; init; }
        public bool UserMustChangePassword { get; private set; }

        public string? DisplayName { get; private set; }
        public string? Email { get; private set; }
        public string? Phone { get; private set; }
        public string? Username { get; private set; }
        public string? UserPrincipalName { get; private set; }
        private CredentialVerificationResult(bool isAuthenticated)
        {
            IsAuthenticated = isAuthenticated;
        }

        public static CredentialVerificationResultBuilder CreateBuilder(bool isAuthenticated)
        {
            return new CredentialVerificationResultBuilder(new CredentialVerificationResult(isAuthenticated));
        }

        public static CredentialVerificationResult ByPass()
        {
            return new CredentialVerificationResult(true)
            {
                IsBypass = true
            };
        }

        public static CredentialVerificationResult FromKnownError(string errorMessage, string? username = null)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                return FromUnknowError();
            }

            var pattern = @"data ([0-9a-e]{3})";
            var match = Regex.Match(errorMessage, pattern);

            if (match.Success && match.Groups.Count == 2)
            {
                var data = match.Groups[1].Value;
                
                var resultBuilder = CreateBuilder(false);
                if(username != null)
                {
                    resultBuilder.SetUsername(username);
                }

                switch (data)
                {
                    case "525":
                        return new CredentialVerificationResult(false) { Reason = "User not found" };
                    case "52e":
                        return new CredentialVerificationResult(false) { Reason = "Invalid credentials" };
                    case "530":
                        return new CredentialVerificationResult(false) { Reason = "Not permitted to logon at this time" };
                    case "531":
                        return new CredentialVerificationResult(false) { Reason = "Not permitted to logon at this workstation​" };
                    case "532":
                        return resultBuilder
                            .SetReason("Password Expired")
                            .SetUserMustChangePassword(true)
                            .Build();
                    case "533":
                        return new CredentialVerificationResult(false) { Reason = "Account disabled" };
                    case "701":
                        return new CredentialVerificationResult(false) { Reason = "Account expired" };
                    case "773":
                        return resultBuilder
                            .SetReason("User must change password")
                            .SetUserMustChangePassword(true)
                            .Build();
                    case "775":
                        return new CredentialVerificationResult(false) { Reason = "User account locked" };
                }
            }

            return FromUnknowError(errorMessage);
        }

        public static CredentialVerificationResult FromUnknowError(string? errorMessage = null)
        {
            return new CredentialVerificationResult(false) { Reason = errorMessage ?? "Unknown error" };
        }

        public class CredentialVerificationResultBuilder
        {
            private readonly CredentialVerificationResult _result;

            public CredentialVerificationResultBuilder(CredentialVerificationResult result)
            {
                _result = result ?? throw new ArgumentNullException(nameof(result));
            }

            public CredentialVerificationResultBuilder SetDisplayName(string? displayName)
            {
                _result.DisplayName = displayName;
                return this;
            }

            public CredentialVerificationResultBuilder SetEmail(string? email)
            {
                _result.Email = email;
                return this;
            }

            public CredentialVerificationResultBuilder SetPhone(string? phone)
            {
                _result.Phone = phone;
                return this;
            }
            
            public CredentialVerificationResultBuilder SetUsername(string? username)
            {
                _result.Username = username;
                return this;
            }

            public CredentialVerificationResultBuilder SetUserMustChangePassword(bool userMustChangePassword)
            {
                _result.UserMustChangePassword = userMustChangePassword;
                return this;
            }

            public CredentialVerificationResultBuilder SetReason(string? reason)
            {
                _result.Reason = reason;
                return this;
            }

            public CredentialVerificationResultBuilder SetUserPrincipalName(string? upn)
            {
                _result.UserPrincipalName = upn;
                return this;
            }


            public CredentialVerificationResult Build()
            {
                return _result;
            }
        }
    }
}
