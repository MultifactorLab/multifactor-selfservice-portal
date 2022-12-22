using LdapForNet;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.Abstractions;
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
        public LdapIdentity? BindedUser { get; }

        private LdapConnectionAdapter(string uri, LdapIdentity? user, LdapConnectionAdapterConfig config)
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

            var bindDn = config.BindIdentityFormatter.FormatIdentity(user, uri);
            await instance._connection.BindAsync(LdapAuthType.Simple, new LdapCredential
            {
                UserName = bindDn,
                Password = config.BindPasswordFormatter.Format(password)
            });
            return instance;
        }

        public static LdapConnectionAdapter CreateAnonymous(string uri, ILogger? logger = null)
        {
            if (uri is null) throw new ArgumentNullException(nameof(uri));

            var config = new LdapConnectionAdapterConfig
            {
                Logger = logger
            };
            var instance = new LdapConnectionAdapter(uri, null, config);
            instance._connection.TrustAllCertificates();

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
            instance._connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);

            instance._connection.Bind(LdapAuthType.Anonymous, null);

            return instance;
        }

        public async Task<LdapServerInfo> GetServerInfoAsync()
        {
            var result = await _connection.SearchAsync(null, "(objectclass=*)",
                        new[]
                        {
                        "namingContexts",
                        "subschemaSubentry",
                        "supportedLDAPVersion",
                        "supportedSASLMechanisms",
                        "supportedExtension",
                        "supportedControl",
                        "supportedFeatures",
                        "vendorName",
                        "vendorVersion"
                        }, LdapSearchScope.LDAP_SCOPE_BASE);
            var entry = result.FirstOrDefault();
            if (entry == null)
            {
                return new LdapServerInfo(LdapImplementation.Unknown);
            }

            var rootDse = await _connection.SearchAsync(null, "(objectclass=*)", scope: LdapSearchScope.LDAP_SCOPE_BASE);
            foreach (var attribute in rootDse.First().DirectoryAttributes)
            {
                entry.DirectoryAttributes.Remove(attribute.Name);
                entry.DirectoryAttributes.Add(attribute);
            }

            var attrs = entry.DirectoryAttributes;
            LdapImplementation impl = LdapImplementation.ActiveDirectory;

            if (attrs.TryGetValue("vendorName", out var vendorName))
            {
                impl = GetImplementationFromVendorName(vendorName.GetValue<string>());
                if (impl == LdapImplementation.Unknown)
                {
                    impl = LdapImplementation.ActiveDirectory;
                }
            }
            else if (attrs.TryGetValue("objectClass", out var objectClass))
            {
                var classes = objectClass.GetValues<string>();
                if (classes.Contains("OpenLDAProotDSE"))
                {
                    impl = LdapImplementation.OpenLdap;
                }
            }

            return new LdapServerInfo(impl);
        }

        private static LdapImplementation GetImplementationFromVendorName(string vendorName)
        {
            if (vendorName.Contains("389 Project", StringComparison.OrdinalIgnoreCase))
            {
                return LdapImplementation.FreeIPA;
            }

            if (vendorName.Contains("Samba", StringComparison.OrdinalIgnoreCase))
            {
                return LdapImplementation.Samba;
            }

            return LdapImplementation.Unknown;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}