using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata.GlobalValues;

namespace MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims;

public static class ClaimValueSourceFactory
{
    public static IClaimValueSource CreateClaimValueSource(string value)
    {
        if ((value.StartsWith('@') || value.StartsWith('$')) && value.Length < 2 || value.Length == 0)
        {
            throw new InvalidClaimConditionException($"Error has occured while parsing the operand {value}. Invalid Format");
        }
            
        if (value.StartsWith('$'))
        {
            var key = value.Substring(1);
            if (!ApplicationGlobalValuesMetadata.HasKey(key))
            {
                throw new InvalidClaimConditionException($"Error has occured while parsing the operand {value}." +
                                                         $" Invalid variable was used: {key}");
            }
            return new ReservedValueClaimValueSource(ApplicationGlobalValuesMetadata.ParseKey(key));
        }

        if (value.StartsWith('@'))
        {
            var key = value.Substring(1);
            return new AttributeClaimValueSource(key);
        }

        return new LiteralClaimValueSource(value);
    }
}