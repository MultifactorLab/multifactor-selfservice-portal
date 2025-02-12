﻿using MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.Connection;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory.Dto;
using System.Reflection;

namespace MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory
{
    /// <summary>
    /// Returns information about application status.
    /// </summary>
    public class GetApplicationInfoStory
    {
        private readonly MultiFactorApi _api;
        private readonly IWebHostEnvironment _env;
        private readonly PortalSettings _settings;
        private readonly IConfiguration _config;
        private readonly ILogger<GetApplicationInfoStory> _logger;
        private readonly IBindIdentityFormatter _bindDnFormatter;
        private readonly ILdapConnectionAdapter _ldapConnectionAdapter;

        public GetApplicationInfoStory(MultiFactorApi api, 
            IWebHostEnvironment env,
            PortalSettings settings, 
            IConfiguration config, 
            ILogger<GetApplicationInfoStory> logger,
            IBindIdentityFormatter bindDnFormatter,
            ILdapConnectionAdapter ldapConnectionAdapter)
        {
            _api = api;
            _env = env;
            _settings = settings;
            _config = config;
            _logger = logger;
            _bindDnFormatter = bindDnFormatter;
            _ldapConnectionAdapter = ldapConnectionAdapter;
        }

        public async Task<ApplicationInfoDto> ExecuteAsync()
        {
            var apiStatus = await GetApiStatusAsync();
            var ldapStatus = await GetLdapStatus();

            var data = new
            {
                TimeStamp = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
                Environment = _config.GetEnvironment(),
                Version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "1.0.0",
                ApiStatus = apiStatus.ToString(),
                LdapServicesStatus = ldapStatus.ToString()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(data);

            _logger.LogInformation("Application status was requested. Result: {json:l}", json);

            var isProd = _env.IsEnvironment(Constants.PRODUCTION_ENV);
            var ok = apiStatus == ApplicationComponentStatus.Ok && ldapStatus == ApplicationComponentStatus.Ok;

            return new ApplicationInfoDto(data.TimeStamp)
            {
                Environment = !isProd ? data.Environment : null,
                Version = !isProd ? data.Version : null,
                ApiStatus = !isProd ? data.ApiStatus : null,
                LdapServicesStatus = !isProd ? data.LdapServicesStatus : null,
                Message = isProd ? $"{(ok ? "Everything is OK" : "Something is WRONG")}. For more information see logs." : null
            };
        }

        private async Task<ApplicationComponentStatus> GetApiStatusAsync()
        {
            try
            {
                await _api.PingAsync();
                return ApplicationComponentStatus.Ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while requesting API status");
                return ApplicationComponentStatus.Unreachable;
            }
        }

        private async Task<ApplicationComponentStatus> GetLdapStatus()
        {
            try
            {
                var user = LdapIdentity.ParseUser(_settings.TechnicalAccountSettings.User!);
                using var conn = await _ldapConnectionAdapter.CreateAsync(_settings.CompanySettings.Domain, user, 
                    _settings.TechnicalAccountSettings.Password!,
                    config => config.SetBindIdentityFormatter(_bindDnFormatter).SetLogger(_logger));
                return ApplicationComponentStatus.Ok;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while requesting LDAP status");
                return ApplicationComponentStatus.Unreachable;
            }
        }
    }
}
