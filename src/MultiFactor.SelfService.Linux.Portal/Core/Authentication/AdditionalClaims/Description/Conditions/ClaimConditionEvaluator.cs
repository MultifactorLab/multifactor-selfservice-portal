namespace MultiFactor.SelfService.Linux.Portal.Core.Authentication.AdditionalClaims.Description.Conditions
{
    public class ClaimConditionEvaluator
    {
        private readonly IApplicationValuesContext _claimValuesContext;
        private readonly ILogger<ClaimConditionEvaluator> _logger;

        public ClaimConditionEvaluator(IApplicationValuesContext claimValuesContext, ILogger<ClaimConditionEvaluator> logger)
        {
            _claimValuesContext = claimValuesContext ?? throw new ArgumentNullException(nameof(claimValuesContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Evaluates condition and returns TRUE or FALSE.
        /// </summary>
        /// <param name="condition">Claim condition</param>
        /// <returns>TRUE / FALSE</returns>
        public bool Evaluate(ClaimCondition condition)
        {
            var leftValue = condition.LeftOperand.GetValues(_claimValuesContext);
            var rightValue = condition.RightOperand.GetValues(_claimValuesContext);

            switch (condition.Operation)
            {
                case ClaimsConditionOperation.Eq:
                    return rightValue.All(x => leftValue.Contains(x, new OrdinalIgnoreCaseStringComparer()));

                default:
                    _logger.LogDebug("Not supported claim condition operation: {op}", condition.Operation);
                    return false;
            }
        }
    }
}
