using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory.ClaimsSources
{
    public class AdditionalClaimsSource : IClaimsSource
    {
        private const string _additionalClaimTypePrefix = "ssp:";

        private readonly AdditionalClaimsMetadata _additionalClaimsMetadata;
        private readonly IApplicationValuesContext _claimValuesContext;
        private readonly ClaimConditionEvaluator _conditionEvaluator;

        public AdditionalClaimsSource(AdditionalClaimsMetadata additionalClaimsMetadata, IApplicationValuesContext claimValuesContext,
            ClaimConditionEvaluator conditionEvaluator)
        {
            _additionalClaimsMetadata = additionalClaimsMetadata ?? throw new ArgumentNullException(nameof(additionalClaimsMetadata));
            _claimValuesContext = claimValuesContext ?? throw new ArgumentNullException(nameof(claimValuesContext));
            _conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));
        }

        public IReadOnlyDictionary<string, string> GetClaims()
        {
            var claims = new Dictionary<string, string>();

            foreach (var descriptor in _additionalClaimsMetadata.Descriptors)
            {
                if (descriptor.Condition == null || _conditionEvaluator.Evaluate(descriptor.Condition))
                {
                    claims[$"{_additionalClaimTypePrefix}{descriptor.Name}"] = GetValue(descriptor.Source.GetValues(_claimValuesContext));
                }
            }

            return claims;
        }

        private string GetValue(IReadOnlyList<string> values)
        {
            switch (values.Count)
            {
                case 0: return string.Empty;
                case 1: return values[0];
                default: return System.Text.Json.JsonSerializer.Serialize(values);
            }
        }
    }
}
