using LdapForNet;
using System.Diagnostics;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap
{
    public class LdapConnectionAdapter : IDisposable
    {
        private readonly LdapConnection _connection;
        private readonly string _uri;
        private readonly ILogger? _logger;

        private LdapConnectionAdapter(string uri, ILogger? logger)
        {
            _connection = new LdapConnection();
            _uri = uri;
            _logger = logger;
        }

        public async Task<LdapIdentity> WhereAmI()
        {
            var queryResult = await SearchQueryAsync("", "(objectclass=*)", LdapSearchScope.LDAP_SCOPE_BASEOBJECT, "defaultNamingContext");
            var result = queryResult.SingleOrDefault() ?? throw new InvalidOperationException($"Unable to query {_uri} for current user");

            var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();
            return new LdapIdentity(defaultNamingContext, IdentityType.DistinguishedName);
        }

        public async Task<IList<LdapEntry>> SearchQueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
        {
            if (_logger == null)
            {
                return await _connection.SearchAsync(baseDn, filter, attributes, scope);
            }

            var sw = Stopwatch.StartNew();
            var searchResult = await _connection.SearchAsync(baseDn, filter, attributes, scope);

            if (sw.Elapsed.TotalSeconds > 2)
            {
                _logger.LogWarning("Slow response while querying {baseDn:l}. Time elapsed {elapsed}", baseDn, sw.Elapsed);
            }

            return searchResult;
        }

        public Task<DirectoryResponse> SendRequestAsync(DirectoryRequest request)
        {
            return _connection.SendRequestAsync(request);
        }

        public static async Task<LdapConnectionAdapter> CreateAsync(string uri, LdapIdentity user, string password, ILogger? logger = null)
        {
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (password is null) throw new ArgumentNullException(nameof(password));

            var instance = new LdapConnectionAdapter(uri, logger);

            // trust self-signed certificates on ldap server
            instance._connection.TrustAllCertificates();

            if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                var ldapUri = new Uri(uri);
                instance._connection.Connect(ldapUri.GetLeftPart(UriPartial.Authority));
            }
            else
            {
                instance._connection.Connect(uri, 389);
            }

            instance._connection.SetOption(LdapOption.LDAP_OPT_PROTOCOL_VERSION, (int)LdapVersion.LDAP_VERSION3);

            // do not follow chase referrals
            instance._connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);

            var bindDn = user.FormatBindDn(uri);
            var escapedPwd = EscapePwdString(password);
            await instance._connection.BindAsync(LdapAuthType.Simple, new LdapCredential
            {
                UserName = bindDn,
                Password = escapedPwd
            });

            return instance;
        }

        private static string EscapePwdString(string input)
        {
            // TODO
            return input;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}