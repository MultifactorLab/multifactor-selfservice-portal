using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core;

namespace MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement
{
    public class PasswordRequirementsService(PortalSettings portalSettings, IEnumerable<PasswordRequirementRule> rules)
    {
        public PasswordValidationResult ValidatePassword(string password)
        {
            if (portalSettings.PasswordRequirements?.PwdRequirement == null)
            {
                return new PasswordValidationResult();
            }

            foreach (var requirement in portalSettings.PasswordRequirements.PwdRequirement.Where(r => r.Enabled))
            {
                if (string.IsNullOrEmpty(requirement.Condition))
                {
                    continue;
                }

                var rule = GetRule(requirement.Condition);

                if (rule == null)
                {
                    continue;
                }

                if (rule.Validate(password))
                {
                    continue;
                }

                return new PasswordValidationResult(rule.GetLocalizedDescription());
            }

            return new PasswordValidationResult();
        }

        public PasswordRequirementRule GetRule(string condition)
        {
            return rules.FirstOrDefault(r => string.Equals(r.RuleName, condition, StringComparison.InvariantCultureIgnoreCase));
        }
    }
} 