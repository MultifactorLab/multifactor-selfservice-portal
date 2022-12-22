namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    public class LdapServerInfo
    {
        public LdapImplementation Implementation { get; }

        public LdapServerInfo(LdapImplementation implementation)
        {
            Implementation = implementation;
        }

    }
}