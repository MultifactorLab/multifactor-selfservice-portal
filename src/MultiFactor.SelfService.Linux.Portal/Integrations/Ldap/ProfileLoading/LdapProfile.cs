using MultiFactor.SelfService.Linux.Portal.Core.LdapAttributesCaching;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.ProfileLoading
{
    public class LdapProfile
    {
        public LdapIdentity BaseDn { get; }
        public string DistinguishedName { get; }

        public string DisplayName => Attributes.GetValue("displayName");
        public string Email => Attributes.GetValue("email");
        public string Phone => Attributes.GetValue("telephoneNumber");
        public string Mobile => Attributes.GetValue("mobile");
        public string Upn => Attributes.GetValue("userPrincipalName");
        public IReadOnlyList<string> MemberOf => Attributes.GetValues("memberOf");

        private readonly LdapAttributesCache _attributes = new();
        public ILdapAttributesCache Attributes => _attributes;

        private LdapProfile(LdapIdentity baseDn, string distinguishedName)
        {
            if (baseDn is null) throw new ArgumentNullException(nameof(baseDn));
            if (distinguishedName is null) throw new ArgumentNullException(nameof(distinguishedName));
            BaseDn = baseDn;
            DistinguishedName = distinguishedName;
        }

        public static LdapProfileBuilder Create(LdapIdentity baseDn, string distinguishedName)
        {
            return new LdapProfileBuilder(new LdapProfile(baseDn, distinguishedName));
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
