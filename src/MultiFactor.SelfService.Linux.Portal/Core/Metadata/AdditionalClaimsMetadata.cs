using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description;

namespace MultiFactor.SelfService.Linux.Portal.Core.Metadata
{
    /// <summary>
    /// Additional claims metadata storage and cache.
    /// </summary>
    public class AdditionalClaimsMetadata
    {
        private IReadOnlyList<AdditionalClaimDescriptor> _additionalClaims;
        private IReadOnlyList<string> _requiredAttributes;
        private readonly AdditionalClaimDescriptorsProvider _descriptorsProvider;

        /// <summary>
        /// Returns all additional claims descriptors.
        /// </summary>
        public IReadOnlyList<AdditionalClaimDescriptor> Descriptors => GetDescriptors();

        /// <summary>
        /// Consume and returns all attributes required by claim value getter or by claim condition evaluator.
        /// </summary>
        public IReadOnlyList<string> RequiredAttributes => GetRequiredAttributes();

        public AdditionalClaimsMetadata(AdditionalClaimDescriptorsProvider descriptorsProvider)
        {
            _descriptorsProvider = descriptorsProvider ?? throw new ArgumentNullException(nameof(descriptorsProvider));
        }

        /// <summary>
        /// Preloads metadata.
        /// </summary>
        public void LoadMetadata()
        {
            GetDescriptors();
            GetRequiredAttributes();
        }

        private IReadOnlyList<string> GetRequiredAttributes()
        {
            if (_requiredAttributes == null)
            {
                _requiredAttributes = GetDescriptors()
                .Select(x => x.Source)
                .Concat(GetDescriptors()
                    .Where(x => x.Condition != null)
                    .SelectMany(x => new[] { x.Condition.LeftOperand, x.Condition.RightOperand }))
                .OfType<AttributeClaimValueSource>()
                .Select(x => x.Attribute)
                .Distinct(new OrdinalIgnoreCaseStringComparer())
                .ToList()
                .AsReadOnly();
            }

            return _requiredAttributes;
        }

        private IReadOnlyList<AdditionalClaimDescriptor> GetDescriptors()
        {
            if (_additionalClaims == null)
            {
                _additionalClaims = _descriptorsProvider.GetDescriptors();
            }

            return _additionalClaims;
        }
    }
}
