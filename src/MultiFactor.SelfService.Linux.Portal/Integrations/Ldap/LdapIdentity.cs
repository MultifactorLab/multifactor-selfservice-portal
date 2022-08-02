namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapIdentity
    {
        public string Name { get; }
        public IdentityType Type { get; }

        public LdapIdentity(string name, IdentityType type)
        {
            Name = name;
            Type = type;
        }

        public static LdapIdentity ParseUser(string name)
        {
            return Parse(name, true);
        }

        public static LdapIdentity ParseGroup(string name)
        {
            return Parse(name, false);
        }

        /// <summary>
        /// Converts domain.local to DC=domain,DC=local
        /// </summary>
        public static LdapIdentity FqdnToDn(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var portIndex = name.IndexOf(":");
            if (portIndex > 0)
            {
                name = name.Substring(0, portIndex);
            }

            var domains = name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var dn = domains.Select(p => $"DC={p}").ToArray();

            return new LdapIdentity(string.Join(",", dn), IdentityType.DistinguishedName);
        }

        /// <summary>
        /// DC part from DN
        /// </summary>
        public static LdapIdentity BaseDn(string dn)
        {
            if (dn is null) throw new ArgumentNullException(nameof(dn));

            var ncs = dn.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var baseDn = ncs.Where(nc => nc.ToLower().StartsWith("dc="));

            return new LdapIdentity(string.Join(",", baseDn), IdentityType.DistinguishedName);
        }

        /// <summary>
        /// Converts DC=domain,DC=local to domain.local
        /// </summary>
        public static string DnToFqdn(string dn)
        {
            if (dn is null) throw new ArgumentNullException(nameof(dn));

            var ncs = dn.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var fqdn = ncs.Select(nc => nc.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd(','));
            return string.Join(".", fqdn);
        }

        /// <summary>
        /// Extracts CN from DN
        /// </summary>
        public static string DnToCn(string dn)
        {
            if (dn is null) throw new ArgumentNullException(nameof(dn));

            return dn.Split(',')[0].Split("=")[1];
        }

        public string DnToFqdn()
        {
            return DnToFqdn(Name);
        }

        public bool IsChildOf(LdapIdentity parent)
        {
            if (parent is null) throw new ArgumentNullException(nameof(parent));
            return Name.EndsWith(parent.Name);
        }

        public string FormatBindDn(string ldapUri)
        {
            if (Type == IdentityType.UserPrincipalName)
            {
                return Name;
            }

            //try create upn from domain name
            if (Uri.IsWellFormedUriString(ldapUri, UriKind.Absolute))
            {
                var uri = new Uri(ldapUri);
                if (uri.PathAndQuery != null && uri.PathAndQuery != "/")
                {
                    var fqdn = DnToFqdn(uri.PathAndQuery);
                    return $"{Name}@{fqdn}";
                }
            }

            return Name;
        }

        private static LdapIdentity Parse(string name, bool isUser)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var identity = name.ToLower();

            //remove DOMAIN\\ prefix
            var index = identity.IndexOf("\\");
            if (index > 0)
            {
                identity = identity.Substring(index + 1);
            }

            if (identity.Contains('='))
            {
                return new LdapIdentity(identity, IdentityType.DistinguishedName);
            }

            if (identity.Contains('@'))
            {
                return new LdapIdentity(identity, IdentityType.UserPrincipalName);
            }

            return new LdapIdentity(identity, isUser ? IdentityType.Uid : IdentityType.Cn);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
