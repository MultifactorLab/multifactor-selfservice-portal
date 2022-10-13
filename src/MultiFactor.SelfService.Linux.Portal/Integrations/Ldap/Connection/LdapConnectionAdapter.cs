using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    public class LdapConnectionAdapter : IDisposable
    {
        private readonly LdapConnection _connection;
        public string Uri { get; }
        private readonly LdapConnectionAdapterConfig _config;

        /// <summary>
        /// Returns user that has been successfully binded with LDAP directory.
        /// </summary>
        public LdapIdentity BindedUser { get; }

        private LdapConnectionAdapter(string uri, LdapIdentity user, LdapConnectionAdapterConfig config)
        {
            _connection = new LdapConnection();
            Uri = uri;
            BindedUser = user;
            _config = config;
        }

        public async Task<LdapDomain> WhereAmIAsync()
        {
            var filter = LdapFilter.Create("objectclass", "*").Build();
            var queryResult = await SearchQueryAsync("", filter, LdapSearchScope.LDAP_SCOPE_BASEOBJECT, "defaultNamingContext");
            var result = queryResult.SingleOrDefault() ?? throw new InvalidOperationException($"Unable to query {Uri} for current user");

            var defaultNamingContext = result.DirectoryAttributes["defaultNamingContext"].GetValue<string>();
            return LdapDomain.Parse(defaultNamingContext);
        }

        public async Task<IList<LdapEntry>> SearchQueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
        {
            if (_config.Logger == null)
            {
                return await _connection.SearchAsync(baseDn, filter, attributes, scope);
            }

            var sw = Stopwatch.StartNew();
            var searchResult = await _connection.SearchAsync(baseDn, filter, attributes, scope);

            if (sw.Elapsed.TotalSeconds > 2)
            {
                _config.Logger.LogWarning("Slow response while querying {baseDn:l}. Time elapsed {elapsed}", baseDn, sw.Elapsed);
            }

            return searchResult;
        }

        public Task<DirectoryResponse> SendRequestAsync(DirectoryRequest request)
        {
            return _connection.SendRequestAsync(request);
        }

        public static async Task<LdapConnectionAdapter> CreateAsync(string uri, LdapIdentity user, string password,
            Action<LdapConnectionAdapterConfigBuilder>? configure)
        {
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (password is null) throw new ArgumentNullException(nameof(password));

            var config = new LdapConnectionAdapterConfig();
            configure?.Invoke(new LdapConnectionAdapterConfigBuilder(config));

            var instance = new LdapConnectionAdapter(uri, user, config);

            // fix for tests running.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // trust self-signed certificates on ldap server
                instance._connection.TrustAllCertificates();
            }

            if (System.Uri.IsWellFormedUriString(uri, UriKind.Absolute))
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

            var bindDn = config.Formatter.FormatBindDn(user, uri);
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