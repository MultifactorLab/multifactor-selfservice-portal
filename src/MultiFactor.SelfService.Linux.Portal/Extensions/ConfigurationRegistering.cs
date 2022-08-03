using FluentValidation;
using Serilog;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class ConfigurationRegistering
    {
        /// <summary>
        /// Loads configuration and adds it as a singletone in the DI container.
        /// Must be called after the logger is configered (ConfigureLogging()).
        /// </summary>
        public static WebApplicationBuilder RegisterConfiguration(this WebApplicationBuilder applicationBuilder)
        {
            try
            {
                var settings = GetSettings(applicationBuilder.Configuration) ?? throw new Exception("Can't find PortalSettings section in appsettings");

                Validate(settings);

                applicationBuilder.Services.AddSingleton(settings);
                return applicationBuilder;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Unable to start");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static PortalSettings GetSettings(IConfigurationRoot config)
        {
            var section = config.GetSection(nameof(PortalSettings));
            return section.Get<PortalSettings>(o =>
            {
                o.BindNonPublicProperties = true;
            });
        }

        private static void Validate(PortalSettings settings)
        {
            var result = new PortalSettingsValidator().Validate(settings);
            if (result.IsValid) return;

            throw new Exception(result.Errors.Select(x => x.ErrorMessage).Aggregate((acc, cur) => $"{acc}{Environment.NewLine}{cur}"));
        }

        private class PortalSettingsValidator : AbstractValidator<PortalSettings>
        {
            public PortalSettingsValidator()
            {
                RuleFor(c => c.CompanyName).NotEmpty().WithMessage(GetErrorMessage(nameof(PortalSettings.CompanyName)));
                RuleFor(c => c.CompanyDomain).NotEmpty().WithMessage(GetErrorMessage(nameof(PortalSettings.CompanyDomain)));
                RuleFor(c => c.CompanyLogoUrl).NotEmpty().WithMessage(GetErrorMessage(nameof(PortalSettings.CompanyLogoUrl)));
                RuleFor(c => c.TechnicalAccUsr).NotEmpty().WithMessage(GetErrorMessage(nameof(PortalSettings.TechnicalAccUsr)));
                RuleFor(c => c.TechnicalAccPwd).NotEmpty().WithMessage(GetErrorMessage(nameof(PortalSettings.TechnicalAccPwd)));
                RuleFor(c => c.MultiFactorApiUrl).NotEmpty().WithMessage(GetErrorMessage(nameof(PortalSettings.MultiFactorApiUrl)));
                RuleFor(c => c.MultiFactorApiKey).NotEmpty().WithMessage(GetErrorMessage(nameof(PortalSettings.MultiFactorApiKey)));
                RuleFor(c => c.MultiFactorApiSecret).NotEmpty().WithMessage(GetErrorMessage(nameof(PortalSettings.MultiFactorApiSecret)));

                RuleFor(c => c.EnablePasswordManagement).Must((model, value) =>
                {
                    if (!value) return true;
                    if (!Uri.IsWellFormedUriString(model.CompanyDomain, UriKind.Absolute)) return false;
                    var uri = new Uri(model.CompanyDomain);
                    return uri.Scheme == "ldaps";
                }).WithMessage($"Need use secure connection for manage password. Please check '{nameof(PortalSettings.CompanyDomain)}' settings property or disable password management ('{nameof(PortalSettings.EnablePasswordManagement)}' property)");
            }

            private static string GetErrorMessage(string propertyName)
            {
                return $"Configuration error: '{propertyName}' element not found";
            }
        }
    }
}
