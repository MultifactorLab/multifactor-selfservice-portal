using Microsoft.Extensions.Caching.Memory;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration.Providers
{
    public class CloudConfigurationProvider : ConfigurationProvider, IConfigurationSource
    {
        private IMultiFactorApi _multifactorApi;
        private IMemoryCache _cache;
        private readonly ILogger _logger;

        public CloudConfigurationProvider(IMultiFactorApi multiFactorApi, IMemoryCache cache, ILogger<CloudConfigurationProvider> logger)
        {
            _multifactorApi = multiFactorApi;
            _cache = cache;
            _logger = logger;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

        public override void Load()
        {
            try
            {
                if (_cache.TryGetValue("ShowcaseSettings", out ShowcaseSettings settings))
                {
                    return;
                }

                var newSettings = _multifactorApi.GetShowcaseSettingsAsync().GetAwaiter().GetResult();
                _cache.Set("ShowcaseSettings", newSettings, TimeSpan.FromMinutes(1));
                SetData(newSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh settings from Multifactor Cloud. Local Showcase settings may be out of date.");
            }
        }

        private void SetData(ShowcaseSettings settings)
        {
            Data["ShowcaseSettings:Enabled"] = settings.Enabled.ToString();
            SetCollection("ShowcaseSettings:Links", settings.Links);

            OnReload();
        }

        private void SetCollection(string key, ShowcaseLink[] elements)
        {
            ResetCollection(key);

            for (int index = 0; index < elements.Length; index++)
            {
                var baseKey = $"{key}:{index}";
                var item = elements[index];

                Data[$"{baseKey}:Url"] = item.Url;
                Data[$"{baseKey}:Title"] = item.Title;
                Data[$"{baseKey}:Image"] = item.Image;
                Data[$"{baseKey}:OpenInNewTab"] = item.OpenInNewTab.ToString();
            }
        }

        private void ResetCollection(string prefix)
        {
            var keysToRemove = Data.Keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var k in keysToRemove)
            {
                Data.Remove(k);
            }
        }
    }
}
