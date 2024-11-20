using MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description.Conditions;

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
        public static ClaimCondition Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) throw new InvalidClaimConditionException(expression);

            var (op, index) = ParseOp(expression);

            var leftPart = expression.Substring(0, index);
            var rightPart = expression.Substring(index + 1);

            if (leftPart == string.Empty || rightPart == string.Empty) throw new InvalidClaimConditionException(expression);

            var left = ClaimValueSourceFactory.CreateClaimValueSource(leftPart);
            var right = ClaimValueSourceFactory.CreateClaimValueSource(rightPart);

            return new ClaimCondition(left, right, op);
        }

        private static (ClaimsConditionOperation op, int index) ParseOp(string expression)
        {
            int index = expression.IndexOf('=');
            if (index != -1) return (ClaimsConditionOperation.Eq, index);
            
            throw new InvalidClaimConditionException(expression);
        }
    }
    
    internal class InvalidClaimConditionException : Exception
    {
        public InvalidClaimConditionException(string expression) : base($"Invalid expression: {expression}") { }
        public InvalidClaimConditionException(string expression, Exception inner) : base($"Invalid expression: {expression}", inner) { }
    }
}
