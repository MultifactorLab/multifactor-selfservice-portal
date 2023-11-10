using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata.GlobalValues;
using MultiFactor.SelfService.Linux.Portal.Extensions;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory.ClaimsSources
{
    public class ClaimValuesContext : IApplicationValuesContext
    {
        private readonly SafeHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationGlobalValuesProvider _globalValuesProvider;

        public ClaimValuesContext(SafeHttpContextAccessor httpContextAccessor, ApplicationGlobalValuesProvider globalValuesProvider)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _globalValuesProvider = globalValuesProvider ?? throw new ArgumentNullException(nameof(globalValuesProvider));
        }

        public IReadOnlyList<string> this[string key] => GetValues(key);

        private IReadOnlyList<string> GetValues(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            }

            if (ApplicationGlobalValuesMetadata.HasKey(key))
            {
                return _globalValuesProvider.GetValues(ApplicationGlobalValuesMetadata.ParseKey(key));
            }

            return _httpContextAccessor.SafeGetLdapAttributes().GetValues(key);
        }
    }
}
