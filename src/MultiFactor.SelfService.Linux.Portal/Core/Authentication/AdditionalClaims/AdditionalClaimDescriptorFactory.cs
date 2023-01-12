using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata.GlobalValues;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims
{
    public static class AdditionalClaimDescriptorFactory
    {
        public static AdditionalClaimDescriptor Create(Claim claim)
        {
            if (claim is null) throw new ArgumentNullException(nameof(claim));
            var name = (claim.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidClaimDescriptionException(claim.Name);

            var source = GetSource(claim);
            var condition = !string.IsNullOrWhiteSpace(claim.When) 
                ? ClaimConditionParser.Parse(claim.When.Trim())
                : null;
            return new AdditionalClaimDescriptor(name, source, condition);
        }

        private static IClaimValueSource GetSource(Claim claim)
        {
            if (!string.IsNullOrEmpty(claim.Value))
            {
                return new LiteralClaimValueSource(claim.Value);
            }

            if (!string.IsNullOrEmpty(claim.From))
            {
                if (ApplicationGlobalValuesMetadata.HasKey(claim.From))
                {
                    return new ReservedValueClaimValueSource(ApplicationGlobalValuesMetadata.ParseKey(claim.From));
                }

                return new AttributeClaimValueSource(claim.From);
            }

            throw new InvalidClaimDescriptionException(claim.Name);
        }
    }

    [Serializable]
    internal class InvalidClaimDescriptionException : Exception
    {
        public InvalidClaimDescriptionException(string? claimName) : base($"Invalid claim '{claimName}' description") { }
        public InvalidClaimDescriptionException(string? claimName, Exception inner) : base($"Invalid claim '{claimName}' description", inner) { }
        protected InvalidClaimDescriptionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
