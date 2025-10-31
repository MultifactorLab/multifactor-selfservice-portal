using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration.Providers
{
    public class CloudConfigurationProvider : ConfigurationProvider, IConfigurationSource
    {
        private readonly IMultiFactorApi _multifactorApi;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;

        public CloudConfigurationProvider(IMultiFactorApi multiFactorApi, IMemoryCache cache, IWebHostEnvironment hostEnvironment, ILogger<CloudConfigurationProvider> logger)
        {
            _multifactorApi = multiFactorApi;
            _hostEnvironment = hostEnvironment;
            _cache = cache;
            _logger = logger;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

        public async override void Load()
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

                await UpdateLogos(newSettings);
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh settings from Multifactor Cloud. Local Showcase settings may be out of date.");
            }
        }

        private async Task UpdateLogos(ShowcaseSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            var localFolder = Path.Combine(_hostEnvironment.WebRootPath, "content", "images", "showcase");
            if (!Directory.Exists(localFolder))
            {
                Directory.CreateDirectory(localFolder);
            }

            var cloudFileNames = settings.Links.Select(x => x.Image).ToArray();
            var localFileNames = Directory.GetFiles(localFolder)
                .Select(file => Path.GetFileName(file))
                .ToArray();

            var missingFiles = cloudFileNames.Except(localFileNames).ToArray();
            foreach (var fileName in missingFiles)
            {
                var data = await _multifactorApi.GetShowcaseLogoAsync(fileName);
                File.WriteAllBytes(Path.Combine(localFolder, fileName), data);
            }

            var extraFiles = localFileNames.Except(cloudFileNames).ToArray();
            foreach (var filename in extraFiles)
            {
                File.Delete(Path.Combine(localFolder, filename));
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
