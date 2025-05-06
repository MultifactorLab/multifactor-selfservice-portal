using Microsoft.Extensions.Localization;
using MultiFactor.SelfService.Linux.Portal.Resources;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core;
using System.Globalization;

namespace MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement;

public class PasswordRequirementLocalizer(IStringLocalizer<PasswordRequirementResource> localizer)
{
    private readonly IStringLocalizer _localizer = localizer;

    public string GetLocalizedDescription(PasswordRequirementRule rule)
    {
        var currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        
        var description = currentCulture == "ru" ? rule.DescriptionRu : rule.DescriptionEn;
        
        if (string.IsNullOrEmpty(description))
        {
            description = _localizer.GetString($"{rule.RuleName}");
        }

        return description;
    }
}