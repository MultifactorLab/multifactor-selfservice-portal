using FluentValidation;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static partial class ConfigurationRegistering
    {
        private class PortalSettingsValidator : AbstractValidator<PortalSettings>
        {
            public PortalSettingsValidator()
            {
                RuleFor(c => c.CompanySettings).NotNull()
                    .WithMessage(GetErrorMessage(x => x.CompanySettings))
                    .ChildRules(x =>
                    {
                        x.RuleFor(r => r.Name).NotEmpty().WithMessage(GetErrorMessage(r => r.CompanySettings.Name));
                        x.RuleFor(r => r.Domain).NotEmpty().WithMessage(GetErrorMessage(r => r.CompanySettings.Domain));
                        x.RuleFor(r => r.LogoUrl).NotEmpty().WithMessage(GetErrorMessage(r => r.CompanySettings.LogoUrl));
                    });

                RuleFor(c => c.TechnicalAccountSettings).NotNull()
                    .WithMessage(GetErrorMessage(x => x.TechnicalAccountSettings))
                    .ChildRules(x =>
                    {
                        x.RuleFor(r => r.User).NotEmpty().WithMessage(GetErrorMessage(r => r.TechnicalAccountSettings.User));
                        x.RuleFor(r => r.Password).NotEmpty().WithMessage(GetErrorMessage(r => r.TechnicalAccountSettings.Password));
                    });

                RuleFor(c => c.MultiFactorApiSettings).NotNull()
                    .WithMessage(GetErrorMessage(x => x.MultiFactorApiSettings))
                    .ChildRules(x =>
                    {
                        x.RuleFor(r => r.ApiUrl).NotEmpty().WithMessage(GetErrorMessage(r => r.MultiFactorApiSettings.ApiUrl));
                        x.RuleFor(r => r.ApiKey).NotEmpty().WithMessage(GetErrorMessage(r => r.MultiFactorApiSettings.ApiKey));
                        x.RuleFor(r => r.ApiSecret).NotEmpty().WithMessage(GetErrorMessage(r => r.MultiFactorApiSettings.ApiSecret));
                    });

                RuleFor(c => c.CaptchaSettings).NotNull()
                    .WithMessage(GetErrorMessage(x => x.CaptchaSettings))
                    .ChildRules(x =>
                    {
                        x.RuleFor(r => r.Key)
                            .Must((model, value) => !model.Enabled || !string.IsNullOrWhiteSpace(value))
                            .WithMessage(GetCaptchaError(x => x.CaptchaSettings.Key));

                        x.RuleFor(r => r.Secret)
                            .Must((model, value) => !model.Enabled || !string.IsNullOrWhiteSpace(value))
                            .WithMessage(GetCaptchaError(r => r.CaptchaSettings.Secret));

                        x.RuleFor(r => r.CaptchaRequired).IsInEnum().WithMessage($"{GetPropPath(x => x.CaptchaSettings.CaptchaRequired)} contains wrong value.");
                    });
                
                RuleFor(x => x.GroupPolicyPreset).Must((model, value) =>
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.SignUpGroups)) return true;
                    return Regex.IsMatch(value.SignUpGroups, Constants.SIGN_UP_GROUPS_REGEX, RegexOptions.IgnoreCase);
                }).WithMessage($"Invalid group names. Please check '{GetPropPath(p => p.GroupPolicyPreset.SignUpGroups)}' settings property and fix syntax errors.");

                RuleFor(portal => portal.PasswordManagement)
                    .NotNull().WithMessage(GetErrorMessage(portal => portal.PasswordManagement))
                    .ChildRules(portalValidation =>
                    {
                        portalValidation.RuleFor(r => r.PasswordChangingSessionCacheSize)
                            .Must((model, value) => value is null ||
                                                 value >= Constants.BYTES_IN_MB && value < (100L * Constants.BYTES_IN_MB /* 100 MB */))
                            .WithMessage($"Invalid password changing session cache size. Please check '{GetPropPath(x => x.PasswordManagement.PasswordChangingSessionCacheSize)} property.'");

                        portalValidation.RuleFor(r => r.PasswordChangingSessionLifetime)
                            .Must((model, value) => value is null ||
                                                    value.Value < TimeSpan.FromDays(10))
                            .WithMessage($"Invalid password changing session lifetime. Please check '{GetPropPath(x => x.PasswordManagement.PasswordChangingSessionLifetime)} property.'");
                    });
                
                RuleFor(portal => portal)
                    .Must((model, value) => model.CaptchaSettings.Enabled && model.PasswordManagement.AllowPasswordRecovery || 
                        !model.PasswordManagement.AllowPasswordRecovery)
                    .WithMessage("Captcha must be enabled for PasswordRecovery page to enable Password Recovery." +
                                 $"Please, either enable the captcha ('{GetPropPath(x => x.CaptchaSettings)}') or disable Password Recovery ('{GetPropPath(x => x.PasswordManagement.AllowPasswordRecovery)}')");
                
                RuleFor(portal => portal).Must((model, value) => {
                    if (!model.PasswordManagement.Enabled) return true;
                    if (string.IsNullOrWhiteSpace(model.CompanySettings?.Domain)) return false;
                    if (!Uri.IsWellFormedUriString(model.CompanySettings.Domain, UriKind.Absolute)) return false;
                    var uri = new Uri(model.CompanySettings.Domain);
                    return uri.Scheme == "ldaps";
                }).WithMessage($"Need secure connection for password management. Please check '{GetPropPath(x => x.CompanySettings.Domain)}' settings property or disable password management ('{nameof(PortalSettings.PasswordManagement.Enabled)}' property).");

                RuleFor(portal => portal)
                    .Must((model, value) => !model.PasswordManagement.AllowPasswordRecovery
                                          || model.PasswordManagement.AllowPasswordRecovery && model.PasswordManagement.Enabled)
                    .WithMessage($"Password management must be enabled before password recovery. Please, check '{GetPropPath(x => x.PasswordManagement.Enabled)}' property");
                
                
                RuleFor(portal => portal.PasswordRequirements)
                    .ChildRules(requirementsValidation =>
                    {
                        requirementsValidation.RuleForEach(r => r.PwdRequirement)
                            .ChildRules(requirement =>
                            {
                                requirement.RuleFor(r => r.Enabled)
                                    .NotNull().WithMessage("Enabled property must be specified for each password requirement");

                                requirement.RuleFor(r => r.Condition)
                                    .Must(condition => string.IsNullOrEmpty(condition) || Constants.PasswordRequirements.GetAllKnownConstants().Contains(condition))
                                    .WithMessage("Invalid condition value. Must be one of: " + 
                                        string.Join(", ", Constants.PasswordRequirements.GetAllKnownConstants()));

                                requirement.RuleFor(r => r.Value)
                                    .Must((model, value) => {
                                        if (model.Condition == Constants.PasswordRequirements.MIN_LENGTH || 
                                            model.Condition == Constants.PasswordRequirements.MAX_LENGTH)
                                        {
                                            return int.TryParse(value, out int length) && length > 0;
                                        }
                                        return true;
                                    })
                                    .WithMessage("Length value must be a positive number");
                            });

                        requirementsValidation.RuleFor(r => r.PwdRequirement)
                            .Must(requirements => {
                                var minLength = requirements.FirstOrDefault(x => x.Condition == Constants.PasswordRequirements.MIN_LENGTH);
                                var maxLength = requirements.FirstOrDefault(x => x.Condition == Constants.PasswordRequirements.MAX_LENGTH);
                                
                                if (minLength != null && maxLength != null)
                                {
                                    return int.TryParse(minLength.Value, out int min) && 
                                           int.TryParse(maxLength.Value, out int max) && 
                                           min < max;
                                }
                                return true;
                            })
                            .WithMessage("Minimum length must be less than maximum length");
                    });
            }

            private static string GetErrorMessage<TProperty>(Expression<Func<PortalSettings, TProperty>> propertySelector)
            {
                var path = ClassPropertyAccessor.GetPropertyPath(propertySelector, ".");
                return $"'{path}' element not found. Please check appsettings file.";
            }

            private static string GetCaptchaError<TProperty>(Expression<Func<PortalSettings, TProperty>> propertySelector)
            {
                var path = ClassPropertyAccessor.GetPropertyPath(propertySelector, ".");
                return $"Captcha Settings '{path}' is required. Please check appsettings file and define this property value or disable captcha verification ('{GetPropPath(x => x.CaptchaSettings.Enabled)}' property).";
            }

            private static string GetPropPath<TProperty>(Expression<Func<PortalSettings, TProperty>> propertySelector)
            {
                return ClassPropertyAccessor.GetPropertyPath(propertySelector, ".");
            }
        }
    }
}
