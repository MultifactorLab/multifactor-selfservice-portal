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
            if (leftValue.Count == 0)
            {
                _logger.LogDebug("Empty left value found while evaluating condition of claim");
            }

            var rightValue = condition.RightOperand.GetValues(_claimValuesContext);
            if (rightValue.Count == 0)
            {
                _logger.LogDebug("Empty right value found while evaluating condition of claim");
            }

            switch (condition.Operation)
            {
                case ClaimsConditionOperation.Eq:
                    return leftValue.All(x => rightValue.Contains(x, new OrdinalIgnoreCaseStringComparer()));

                default:
                    _logger.LogDebug("Not supported claim condition operation: {op}", condition.Operation);
                    return false;
            }
        }
    }
}
