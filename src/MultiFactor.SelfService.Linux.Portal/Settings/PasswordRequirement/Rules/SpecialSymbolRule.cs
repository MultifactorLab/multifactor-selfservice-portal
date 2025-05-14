using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core;

namespace MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Rules
{
    public class SpecialSymbolRule(
        string descriptionEn = null, 
        string descriptionRu = null, 
        string condition = null,
        PasswordRequirementLocalizer localizer = null)
        : PasswordRequirementRule(descriptionEn, descriptionRu, condition, localizer)
    {
        public override bool Validate(string rawPassword)
        {
            return rawPassword?.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)) ?? false;
        }
    }
}