using MultiFactor.SelfService.Linux.Portal.Core.LdapAttributesCaching;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading
{
    public class LdapProfile
    {
        private readonly PortalSettings _settings;

        public LdapIdentity BaseDn { get; }
        public string DistinguishedName { get; }

        public string DisplayName => Attributes.GetValue("displayName");

        public string Email
        {
            get
            {
                var email = Attributes.GetValue("email");
                if (string.IsNullOrEmpty(email))
                {
                    email = Attributes.GetValue("mail");
                }

                return email;
            }
        }


        public string Phone => Attributes.GetValue("telephoneNumber");
        public string Mobile => Attributes.GetValue("mobile");
        public string Upn => Attributes.GetValue("userPrincipalName");
        public bool UseCustomIdentity => !string.IsNullOrWhiteSpace(_settings.ActiveDirectorySettings.UseAttributeAsIdentity) || _settings.ActiveDirectorySettings.UseUpnAsIdentity;

        public string CustomIdentity
        {
            get
            {
                if (!UseCustomIdentity)
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(_settings.ActiveDirectorySettings.UseAttributeAsIdentity))
                {
                    return Attributes.GetValue(_settings.ActiveDirectorySettings.UseAttributeAsIdentity);
                }

                if (_settings.ActiveDirectorySettings.UseUpnAsIdentity)
                {
                    return Upn;
                }

                throw new InvalidOperationException("Empty custom identity.");
            }
        }
        public IReadOnlyList<string> MemberOf => Attributes.GetValues("memberOf");

        private readonly LdapAttributesCache _attributes = new();
        public ILdapAttributesCache Attributes => _attributes;

        private LdapProfile(LdapIdentity baseDn, string distinguishedName, PortalSettings settings)
        {
            BaseDn = baseDn ?? throw new ArgumentNullException(nameof(baseDn));
            DistinguishedName = distinguishedName ?? throw new ArgumentNullException(nameof(distinguishedName));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool UserMustChangePassword()
        {
            // = "User must change password at next logon" setting
            var userMustChangePasswordHasValue = int.TryParse(Attributes.GetValue("pwdLastSet"), out var pwdLastSet);
            if (userMustChangePasswordHasValue && pwdLastSet == 0)
                return true;

            return PasswordExpirationDate() < DateTime.Now;
        }

        public DateTime PasswordExpirationDate()
        {
            if (PasswordExpirationRawValue == null ||
                !long.TryParse(PasswordExpirationRawValue, out var passwordExpirationInt))
            {
                return DateTime.MaxValue;
            }

            try
            {
                return DateTime.FromFileTime(passwordExpirationInt);
            }
            catch (ArgumentOutOfRangeException aore)
            {
                // inconsistency between the parsing function and AD value
                return DateTime.MaxValue;
            }
        }

        private string PasswordExpirationRawValue => Attributes.GetValue("msDS-UserPasswordExpiryTimeComputed");

        public static LdapProfileBuilder Create(LdapIdentity baseDn, string distinguishedName, PortalSettings settings)
        {
            return new LdapProfileBuilder(new LdapProfile(baseDn, distinguishedName, settings));
        }

        public class LdapProfileBuilder
        {
            private readonly LdapProfile _profile;

            public LdapProfileBuilder(LdapProfile profile)
            {
                _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            }

            public LdapProfileBuilder AddAttribute(string attr, IEnumerable<string> values)
            {
                _profile._attributes.AddAttribute(attr, values);
                return this;
            }

            public LdapProfile Build()
            {
                return _profile;
            }
        }
    }
}
