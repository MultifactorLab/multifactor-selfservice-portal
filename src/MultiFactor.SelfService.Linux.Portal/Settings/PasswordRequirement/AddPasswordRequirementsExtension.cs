using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Rules;

namespace MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement
{
    public static class AddPasswordRequirementsExtension
    {
        public static IServiceCollection AddPasswordRequirements(this IServiceCollection services)
        {
            services.AddSingleton<PasswordRequirementsService>();
            services.AddSingleton<PasswordRequirementLocalizer>();

            services.AddSingleton<IEnumerable<PasswordRequirementRule>>(sp =>
            {
                var settings = sp.GetRequiredService<PortalSettings>();
                var rules = new List<PasswordRequirementRule>();
                var requirements = settings.PasswordRequirements;
                var localizer = sp.GetRequiredService<PasswordRequirementLocalizer>();
                if (requirements != null)
                {
                    foreach (var req in requirements.PwdRequirement.Where(r => r.Enabled))
                    {
                        switch (req.Condition?.ToLowerInvariant())
                        {
                            case Constants.PasswordRequirements.UPPER_CASE_LETTERS:
                                rules.Add(
                                    new UpperCaseLetterRule(req.DescriptionEn, req.DescriptionRu, req.Condition, localizer));
                                break;
                            case Constants.PasswordRequirements.LOWER_CASE_LETTERS:
                                rules.Add(
                                    new LowerCaseLetterRule(req.DescriptionEn, req.DescriptionRu, req.Condition, localizer));
                                break;
                            case Constants.PasswordRequirements.DIGITS:
                                rules.Add(
                                    new DigitRule(req.DescriptionEn, req.DescriptionRu, req.Condition, localizer));
                                break;
                            case Constants.PasswordRequirements.SPECIAL_SYMBOLS:
                                rules.Add(
                                    new SpecialSymbolRule(req.DescriptionEn, req.DescriptionRu, req.Condition, localizer));
                                break;
                            case Constants.PasswordRequirements.MIN_LENGTH:
                                if (int.TryParse(req.Value, out int minLength))
                                {
                                    rules.Add(
                                        new MinLengthRule(minLength, req.DescriptionEn, req.DescriptionRu, req.Condition, localizer));
                                }
                                break;
                            case Constants.PasswordRequirements.MAX_LENGTH:
                                if (int.TryParse(req.Value, out int maxLength))
                                {
                                    rules.Add(
                                        new MaxLengthRule(maxLength, req.DescriptionEn, req.DescriptionRu, req.Condition, localizer));
                                }
                                break;
                        }
                    }
                }
                return rules;
            });
            return services;
        }
    }
}