namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapProfile
    {
        public LdapIdentity BaseDn { get; }
        public string DistinguishedName { get; }

        public string? DisplayName { get; private set; }
        public string? Email { get; private set; }
        public string? Phone { get; private set; }
        public string? Mobile { get; private set; }

        private readonly List<string> _memberOf = new();
        public IReadOnlyList<string> MemberOf => _memberOf;

        public IReadOnlyDictionary<string, object> LdapAttrs { get; } = new Dictionary<string, object>();

        private LdapProfile(LdapIdentity baseDn, string distinguishedName)
        {
            if (baseDn is null) throw new ArgumentNullException(nameof(baseDn));
            if (distinguishedName is null) throw new ArgumentNullException(nameof(distinguishedName));
            BaseDn = baseDn;
            DistinguishedName = distinguishedName;
        }

        public static ActiveDirectoryProfileBuilder CreateBuilder(LdapIdentity baseDn, string distinguishedName)
        {
            return new ActiveDirectoryProfileBuilder(new LdapProfile(baseDn, distinguishedName));
        }

        public class ActiveDirectoryProfileBuilder
        {
            private readonly LdapProfile _profile;

            public ActiveDirectoryProfileBuilder(LdapProfile profile)
            {
                _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            }

            public ActiveDirectoryProfileBuilder SetDisplayName(string displayName)
            {
                _profile.DisplayName = displayName;
                return this;
            }

            public ActiveDirectoryProfileBuilder SetEmail(string email)
            {
                _profile.Email = email;
                return this;
            }

            public ActiveDirectoryProfileBuilder SetPhone(string phone)
            {
                _profile.Phone = phone;
                return this;
            }

            public ActiveDirectoryProfileBuilder SetMobile(string mobile)
            {
                _profile.Mobile = mobile;
                return this;
            }

            public ActiveDirectoryProfileBuilder AddMemberOfValues(string[] values)
            {
                _profile._memberOf.AddRange(values);
                return this;
            }

            public LdapProfile Build()
            {
                return _profile;
            }
        }
    }
}
