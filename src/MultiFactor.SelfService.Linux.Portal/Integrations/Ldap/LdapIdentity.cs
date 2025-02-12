﻿namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapIdentity
    {
        public string Name { get; }
        public IdentityType Type { get; }

        private static readonly char[] DnSeparator = ['='];
        private static readonly char[] DotSeparator = ['.'];
        private static readonly char[] CommaSeparator = [','];

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

            var portIndex = name.IndexOf(':');
            if (portIndex > 0)
            {
                name = name[..portIndex];
            }

            var domains = name.Split(DotSeparator, StringSplitOptions.RemoveEmptyEntries);
            var dn = domains.Select(p => $"DC={p}").ToArray();

            return new LdapIdentity(string.Join(",", dn), IdentityType.DistinguishedName);
        }

        /// <summary>
        /// DC part from DN
        /// </summary>
        public static LdapIdentity BaseDn(string dn)
        {
            ArgumentNullException.ThrowIfNull(dn);

            var ncs = dn.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
            var baseDn = ncs.Where(nc => nc.StartsWith("dc=", StringComparison.CurrentCultureIgnoreCase));

            return new LdapIdentity(string.Join(",", baseDn), IdentityType.DistinguishedName);
        }

        /// <summary>
        /// Converts DC=domain,DC=local to domain.local
        /// </summary>
        public static string DnToFqdn(string dn)
        {
            ArgumentNullException.ThrowIfNull(dn);

            var ncs = dn.Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
            var fqdn = ncs.Select(nc => nc.Split(DnSeparator, StringSplitOptions.RemoveEmptyEntries)[1].TrimEnd(','));
            return string.Join(".", fqdn);
        }

        /// <summary>
        /// Extracts CN from DN
        /// </summary>
        public static string DnToCn(string dn)
        {
            ArgumentNullException.ThrowIfNull(dn);

            return dn.Split(',')[0].Split("=")[1];
        }

        public string DnToFqdn()
        {
            return DnToFqdn(Name);
        }

        public bool IsChildOf(LdapIdentity parent)
        {
            ArgumentNullException.ThrowIfNull(parent);
            return Name.EndsWith(parent.Name);
        }

        private static LdapIdentity Parse(string name, bool isUser)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var identity = name.ToLower();

            //remove DOMAIN\\ prefix
            var index = identity.IndexOf('\\');
            if (index > 0)
            {
                identity = identity[(index + 1)..];
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

        public string GetUid()
        {
            switch (Type)
            {
                case IdentityType.Uid: return Name;
                case IdentityType.UserPrincipalName: return UpnToUid(Name);
                default: throw new InvalidOperationException("Identity should be of type UID or UPN");
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public bool IsEquivalentTo(LdapIdentity identity)
        {
            if (identity == null) return false;
            if (identity == this) return true;
            if (identity.Type == IdentityType.DistinguishedName && Type == IdentityType.DistinguishedName)
            {
                return identity.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);
            }
            else if (identity.Type == IdentityType.DistinguishedName || Type == IdentityType.DistinguishedName)
            {
                return false;
            }
            return identity.GetUid().Equals(GetUid(), StringComparison.OrdinalIgnoreCase);
        }

        private static string UpnToUid(string upn)
        {
            if (string.IsNullOrWhiteSpace(upn)) throw new ArgumentException($"'{nameof(upn)}' cannot be null or whitespace.", nameof(upn));

            var index = upn.IndexOf('@');
            if (index == -1) throw new InvalidOperationException("Identity should be of UPN type");

            return upn[..index];
        }
    }
}
