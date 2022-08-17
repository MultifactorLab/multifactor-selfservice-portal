using FluentValidation;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Settings;
using System.Linq.Expressions;

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

                RuleFor(c => c.GoogleReCaptchaSettings).NotNull()
                    .WithMessage(GetErrorMessage(x => x.GoogleReCaptchaSettings))
                    .ChildRules(x =>
                    {
                        x.RuleFor(r => r.Key)
                            .Must((model, value) =>
                            {
                                return !model.Enabled || !string.IsNullOrWhiteSpace(value);
                            })
                            .WithMessage(GetCaptchaError(x => x.GoogleReCaptchaSettings.Key));

                        x.RuleFor(r => r.Secret)
                            .Must((model, value) => !model.Enabled || !string.IsNullOrWhiteSpace(value))
                            .WithMessage(GetCaptchaError(r => r.GoogleReCaptchaSettings.Secret));
                    });

                RuleFor(c => c.EnablePasswordManagement).Must((model, value) =>
                {
                    if (!value) return true;
                    if (string.IsNullOrWhiteSpace(model.CompanySettings?.Domain)) return false;
                    if (!Uri.IsWellFormedUriString(model.CompanySettings.Domain, UriKind.Absolute)) return false;
                    var uri = new Uri(model.CompanySettings.Domain);
                    return uri.Scheme == "ldaps";
                }).WithMessage($"Need secure connection for password management. Please check '{GetPropPath(x => x.CompanySettings.Domain)}' settings property or disable password management ('{nameof(PortalSettings.EnablePasswordManagement)}' property).");
            }

            private static string GetErrorMessage<TProperty>(Expression<Func<PortalSettings, TProperty>> propertySelector)
            {
                var path = ClassPropertyAccessor.GetPropertyPath(propertySelector, ".");
                return $"'{path}' element not found. Please check appsettings file.";
            }

            private static string GetCaptchaError<TProperty>(Expression<Func<PortalSettings, TProperty>> propertySelector)
            {
                var path = ClassPropertyAccessor.GetPropertyPath(propertySelector, ".");
                return $"Google reCaptcha2 '{path}' is required. Please check appsettings file and define this property value or disable captcha verification ('{GetPropPath(x => x.GoogleReCaptchaSettings.Enabled)}' property).";
            }

            private static string GetPropPath<TProperty>(Expression<Func<PortalSettings, TProperty>> propertySelector)
            {
                return ClassPropertyAccessor.GetPropertyPath(propertySelector, ".");
            }
        }
    }
}
