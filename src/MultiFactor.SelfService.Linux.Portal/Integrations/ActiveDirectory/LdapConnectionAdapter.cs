using LdapForNet;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory
{
    public class LdapConnectionAdapter : IDisposable
    {
        private readonly LdapConnection _connection;
        private readonly string _uri;

        private LdapConnectionAdapter(string uri)
        {
            _connection = new LdapConnection();
            _uri = uri;
        }

        public async Task<LdapIdentity> WhereAmI()
        {
            var queryResult = await QueryAsync("", "(objectclass=*)", LdapSearchScope.LDAP_SCOPE_BASEOBJECT, "defaultNamingContext");
            var result = queryResult.SingleOrDefault() ?? throw new InvalidOperationException($"Unable to query {_uri} for current user");

            var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();
            return new LdapIdentity(defaultNamingContext, IdentityType.DistinguishedName);
        }

        public Task<IList<LdapEntry>> QueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
        {
            return _connection.SearchAsync(baseDn, filter, attributes, scope);
        }

        public static async Task<LdapConnectionAdapter> CreateAsync(string uri, LdapIdentity user, string password)
        {
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (password is null) throw new ArgumentNullException(nameof(password));

            var instance = new LdapConnectionAdapter(uri);

            // TODO: trust self-signed certificates on ldap server
            //connection.TrustAllCertificates();

            if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                var ldapUri = new Uri(uri);
                instance._connection.Connect(ldapUri.GetLeftPart(UriPartial.Authority));
            }
            else
            {
                instance._connection.Connect(uri, 389);
            }

            var ldapVersion = (int)LdapVersion.LDAP_VERSION3;
            instance._connection.SetOption(LdapOption.LDAP_OPT_PROTOCOL_VERSION, ldapVersion);

            // do not follow chase referrals
            instance._connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);

            var bindDn = FormatBindDn(uri, user);
            await instance._connection.BindAsync(LdapAuthType.Simple, new LdapCredential
            {
                UserName = bindDn,
                Password = password
            });

            return instance;
        }

        private static string FormatBindDn(string ldapUri, LdapIdentity user)
        {
            if (user.Type == IdentityType.UserPrincipalName)
            {
                return user.Name;
            }

            //try create upn from domain name
            if (Uri.IsWellFormedUriString(ldapUri, UriKind.Absolute))
            {
                var uri = new Uri(ldapUri);
                if (uri.PathAndQuery != null && uri.PathAndQuery != "/")
                {
                    var fqdn = LdapIdentity.DnToFqdn(uri.PathAndQuery);
                    return $"{user.Name}@{fqdn}";
                }
            }

            return user.Name;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}