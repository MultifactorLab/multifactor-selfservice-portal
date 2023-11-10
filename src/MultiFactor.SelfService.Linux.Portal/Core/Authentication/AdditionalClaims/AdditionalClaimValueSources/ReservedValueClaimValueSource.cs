using MultiFactor.SelfService.Linux.Portal.Core.Metadata.GlobalValues;

namespace MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources
{
    public class ReservedValueClaimValueSource : IClaimValueSource
    {
        public ApplicationGlobalValue Value { get; }

        public ReservedValueClaimValueSource(ApplicationGlobalValue value)
        {
            Value = value;
        }

        public IReadOnlyList<string> GetValues(IApplicationValuesContext context)
        {
            return context[Value.ToString()];
        }
    }
}
