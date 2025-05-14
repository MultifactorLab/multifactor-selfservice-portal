namespace MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core
{
    /// <summary>
    /// Atomic password requirement rule.
    /// </summary>
    public abstract class PasswordRequirementRule
    {
        private readonly string _descriptionEn;
        private readonly string _descriptionRu;
        private readonly string _condition;
        private readonly PasswordRequirementLocalizer _localizer;

        protected PasswordRequirementRule(
            string descriptionEn = null, 
            string descriptionRu = null, 
            string condition = null,
            PasswordRequirementLocalizer localizer = null)
        {
            _descriptionEn = descriptionEn;
            _descriptionRu = descriptionRu;
            _condition = condition;
            _localizer = localizer;
        }

        /// <summary>
        /// Name of rule.
        /// </summary>
        public virtual string RuleName => _condition ?? GetType().Name;
        
        /// <summary>
        /// Description in English
        /// </summary>
        public string DescriptionEn => _descriptionEn;
        
        /// <summary>
        /// Description in Russian
        /// </summary>
        public string DescriptionRu => _descriptionRu;

        /// <summary>
        /// Returns localized description of the rule
        /// </summary>
        public virtual string GetLocalizedDescription()
        {
            return _localizer?.GetLocalizedDescription(this);
        }

        /// <summary>
        /// Returns TRUE if the password and user are comply with the terms of the policy rule.
        /// </summary>
        /// <param name="rawPassword">Raw (not encoded) user password.</param>
        /// <returns>True or False.</returns>
        public abstract bool Validate(string rawPassword);
    }
}
