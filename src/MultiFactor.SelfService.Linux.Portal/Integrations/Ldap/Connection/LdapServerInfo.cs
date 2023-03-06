namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    public class LdapServerInfo
    {
        public LdapImplementation Implementation { get; }

        public static LdapServerInfo Default => new(LdapImplementation.ActiveDirectory);

        public LdapServerInfo(LdapImplementation implementation)
        {
            Implementation = implementation;
        }

        public override string ToString()
        {
            return Implementation.ToString();
        }
    }
}