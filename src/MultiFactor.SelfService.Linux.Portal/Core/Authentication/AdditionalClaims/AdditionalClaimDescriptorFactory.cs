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
            ArgumentNullException.ThrowIfNull(claim);
            var name = (claim.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidClaimDescriptionException(claim.Name);
            if (string.IsNullOrWhiteSpace(claim.Value)) throw new InvalidClaimDescriptionException(claim.Name);
            var source = ClaimValueSourceFactory.CreateClaimValueSource(claim.Value);
            var condition = !string.IsNullOrWhiteSpace(claim.When) 
                ? ClaimConditionParser.Parse(claim.When.Trim())
                : null;
            return new AdditionalClaimDescriptor(name, source, condition);
        }
    }
    
    internal class InvalidClaimDescriptionException : Exception
    {
        public InvalidClaimDescriptionException(string claimName) : base($"Invalid claim '{claimName}' description") { }
        public InvalidClaimDescriptionException(string claimName, Exception inner) : base($"Invalid claim '{claimName}' description", inner) { }
    }
}
