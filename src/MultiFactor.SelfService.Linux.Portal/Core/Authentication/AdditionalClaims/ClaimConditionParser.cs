using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.AdditionalClaimValueSources;
using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;
using MultiFactor.SelfService.Linux.Portal.Core.Metadata.GlobalValues;

namespace MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims
{
    public static class ClaimConditionParser
    {
        /// <summary>
        /// Construct condition object from the string representation.
        /// </summary>
        /// <param name="expression">Claim condition string representation.</param>
        /// <returns>Claim condition.</returns>
        /// <exception cref="InvalidClaimConditionException"></exception>
        public static ClaimCondition Parse(string? expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new InvalidClaimConditionException(expression);

            var index = expression.IndexOf('=');
            if (index == -1) throw new InvalidClaimConditionException(expression);

            var leftPart = expression.Substring(0, index);
            var rightPart = expression.Substring(index + 1);

            if (leftPart == string.Empty || rightPart == string.Empty) throw new InvalidClaimConditionException(expression);

            var left = GetOperandSource(leftPart);
            var right = GetOperandSource(rightPart);

            return new ClaimCondition(left, right, ClaimsConditionOperation.Eq);
        }

        private static IClaimValueSource GetOperandSource(string value)
        {
            var leftB = value.IndexOf("'");
            var rightB = value.LastIndexOf("'");
            if (leftB == 0 && rightB == value.Length - 1 && value.Length > 2)
            {
                return new LiteralClaimValueSource(value.Substring(1, rightB - 1));
            }

            if (ApplicationGlobalValuesMetadata.HasKey(value))
            {
                return new ReservedValueClaimValueSource(ApplicationGlobalValuesMetadata.ParseKey(value));
            }

            return new AttributeClaimValueSource(value);
        }
    }

    [Serializable]
    internal class InvalidClaimConditionException : Exception
    {
        public InvalidClaimConditionException(string? expression) : base($"Invalid expression: {expression}") { }
        public InvalidClaimConditionException(string? expression, Exception inner) : base($"Invalid expression: {expression}", inner) { }
        protected InvalidClaimConditionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
