using LdapForNet;
using Microsoft.Extensions.Caching.Memory;
using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding;
using System.Diagnostics;
using static LdapForNet.Native.Native;

namespace MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection
{
    public interface ILdapConnectionAdapter: IDisposable
    {
        LdapIdentity BindedUser { get; }
        Task<LdapDomain> WhereAmIAsync();
        Task<IList<LdapEntry>> SearchQueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes);
        Task<DirectoryResponse> SendRequestAsync(DirectoryRequest request);
        Task<LdapServerInfo> GetServerInfoAsync();
        Task<ILdapConnectionAdapter> CreateAsync(
            string uri,
            LdapIdentity user,
            string password,
            IMemoryCache memoryCache,
            Action<LdapConnectionAdapterConfigBuilder> configure = null);

        ILdapConnectionAdapter CreateAnonymous(
            string uri,
            IMemoryCache memoryCache,
            Action<LdapConnectionAdapterConfigBuilder> configure = null);
    }

    public class LdapConnectionAdapter : ILdapConnectionAdapter
    {
        private readonly LdapConnection _connection;
        private string Uri { get; }
        private readonly LdapConnectionAdapterConfig _config;
        private readonly string[] _namingContextAttributeNames = ["defaultNamingContext", "namingContexts"];
        private IMemoryCache _memoryCache;

        /// <summary>
        /// Returns user that has been successfully binded with LDAP directory.
        /// </summary>
        public LdapIdentity BindedUser { get; private set; }

        private static readonly string[] Attributes =
        [
            "namingContexts",
            "subschemaSubentry",
            "supportedLDAPVersion",
            "supportedSASLMechanisms",
            "supportedExtension",
            "supportedControl",
            "supportedFeatures",
            "vendorName",
            "vendorVersion"
        ];

        private LdapConnectionAdapter(string uri, LdapIdentity user, LdapConnectionAdapterConfig config, IMemoryCache memoryCache)
        {
            _connection = new LdapConnection();
            Uri = uri;
            BindedUser = user;
            _config = config;
            _memoryCache = memoryCache;
        }

        public LdapConnectionAdapter()
        {
        }

        public async Task<LdapDomain> WhereAmIAsync()
        {
            if (_memoryCache.TryGetValue(Uri, out LdapDomain cachedData))
            {
                _config.Logger.LogDebug("Query {method:l} result from cache.", nameof(WhereAmIAsync));
                return cachedData;
            }

            var serverInfo = await GetServerInfoAsync();
            var filter = LdapFilter.Create("objectclass", "*").Build();
            var queryResult = await SearchQueryAsync(string.Empty, filter, LdapSearchScope.LDAP_SCOPE_BASEOBJECT,
                _namingContextAttributeNames);
            var result = queryResult.SingleOrDefault() ??
                         throw new InvalidOperationException($"Unable to query {Uri} for current user");

            string defaultNamingContext = string.Empty;
            foreach (var contextName in _namingContextAttributeNames)
            {
                if (result.DirectoryAttributes.TryGetValue(contextName, out var searchResultAttribute))
                {
                    defaultNamingContext = searchResultAttribute.GetValue<string>();
                    break;
                }
            }

            var domain = LdapDomain.Parse(defaultNamingContext);
            
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(1)
                .SetSlidingExpiration(TimeSpan.FromMinutes(60));
            _memoryCache.Set(Uri, domain, cacheEntryOptions);

            return domain;
        }

        public async Task<IList<LdapEntry>> SearchQueryAsync(string baseDn, string filter, LdapSearchScope scope, params string[] attributes)
        {
            if (_config.Logger == null)
            {
                return await _connection.SearchAsync(baseDn, filter, attributes, scope);
            }

            var sw = Stopwatch.StartNew();
            var searchResult = await _connection.SearchAsync(baseDn, filter, attributes, scope);

            _config.Logger.LogDebug("Querying {baseDn:l} {filter:l}. Time elapsed {elapsed}", baseDn, filter, sw.Elapsed);

            if (sw.Elapsed.TotalSeconds > 2)
            {
                _config.Logger.LogWarning("Slow response while querying {baseDn:l}. Time elapsed {elapsed}", baseDn,
                    sw.Elapsed);
            }

            return searchResult;
        }

        public Task<DirectoryResponse> SendRequestAsync(DirectoryRequest request)
        {
            var sw = Stopwatch.StartNew();
            var result = _connection.SendRequestAsync(request);
            _config.Logger.LogDebug("Querying {request:l}. Time elapsed {elapsed}", request.GetType(), sw.Elapsed);

            return result;
        }

        public async Task<ILdapConnectionAdapter> CreateAsync(
            string uri,
            LdapIdentity user,
            string password,
            IMemoryCache memoryCache,
            Action<LdapConnectionAdapterConfigBuilder> configure = null)
        {
            ArgumentNullException.ThrowIfNull(uri);
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(password);

            var config = new LdapConnectionAdapterConfig();
            configure?.Invoke(new LdapConnectionAdapterConfigBuilder(config));

            var instance = new LdapConnectionAdapter(uri, user, config, memoryCache);

            var sw = Stopwatch.StartNew();
            if (System.Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                var ldapUri = new Uri(uri);
                instance._connection.Connect(ldapUri.GetLeftPart(UriPartial.Authority));
            }
            else
            {
                instance._connection.Connect(uri, 389);
            }

            // MUST set BEFORE TrustAllCertificates
            instance._connection.SetOption(LdapOption.LDAP_OPT_PROTOCOL_VERSION, (int)LdapVersion.LDAP_VERSION3);
            // do not follow chase referrals
            instance._connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);
            // trust self-signed certificates on ldap server
            instance._connection.TrustAllCertificates();

            var bindDn = config.BindIdentityFormatter.FormatIdentity(user, uri);
            await instance._connection.BindAsync(LdapAuthType.Simple, new LdapCredential
            {
                UserName = bindDn,
                Password = config.BindPasswordFormatter.Format(password)
            });

            instance._config.Logger.LogDebug("Connection {method:l}. Time elapsed {elapsed}", nameof(CreateAsync), sw.Elapsed);

            return instance;
        }

        public ILdapConnectionAdapter CreateAnonymous(
            string uri,
            IMemoryCache memoryCache,
            Action<LdapConnectionAdapterConfigBuilder> configure = null)
        {
            ArgumentNullException.ThrowIfNull(uri);

            var config = new LdapConnectionAdapterConfig();
            configure?.Invoke(new LdapConnectionAdapterConfigBuilder(config));

            var instance = new LdapConnectionAdapter(uri, null, config, memoryCache);

            var sw = Stopwatch.StartNew();
            if (System.Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                var ldapUri = new Uri(uri);
                instance._connection.Connect(ldapUri.GetLeftPart(UriPartial.Authority));
            }
            else
            {
                instance._connection.Connect(uri, 389);
            }

            // MUST set BEFORE TrustAllCertificates
            instance._connection.SetOption(LdapOption.LDAP_OPT_PROTOCOL_VERSION, (int)LdapVersion.LDAP_VERSION3);
            instance._connection.SetOption(LdapOption.LDAP_OPT_REFERRALS, IntPtr.Zero);
            // trust self-signed certificates on ldap server
            instance._connection.TrustAllCertificates();
            instance._connection.Bind(LdapAuthType.Anonymous, new LdapCredential());

            instance._config.Logger.LogDebug("Connection {method:l}. Time elapsed {elapsed}", nameof(CreateAnonymous), sw.Elapsed);

            return instance;
        }

        public async Task<LdapServerInfo> GetServerInfoAsync()
        {
            var sw = Stopwatch.StartNew();
            var result = await _connection.SearchAsync(string.Empty, "(objectclass=*)",
                Attributes, LdapSearchScope.LDAP_SCOPE_BASE);
            _config.Logger.LogDebug("Querying {method:l}. Time elapsed {elapsed}", nameof(GetServerInfoAsync),  sw.Elapsed);

            var entry = result.FirstOrDefault();
            if (entry == null)
            {
                var def = LdapServerInfo.Default;
                _config.Logger?.LogWarning("Unknown directory implementation. Using default value: {def}", def);
                return def;
            }

            sw.Restart();
            var rootDse = await _connection.SearchAsync(string.Empty, "(objectclass=*)",
                scope: LdapSearchScope.LDAP_SCOPE_BASE);
            _config.Logger.LogDebug("Querying {method:l} rootDse. Time elapsed {elapsed}", nameof(GetServerInfoAsync), sw.Elapsed);

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