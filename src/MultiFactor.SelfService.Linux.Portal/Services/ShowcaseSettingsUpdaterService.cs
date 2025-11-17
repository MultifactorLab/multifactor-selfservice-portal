using Microsoft.Extensions.Options;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Services
{
    public class ShowcaseSettingsUpdaterService : BackgroundService
    {
        private readonly IMultiFactorApi _multifactorApi;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IOptionsMonitorCache<ShowcaseSettings> _cache;
        private readonly ILogger<ShowcaseSettingsUpdaterService> _logger;
        private readonly TimeSpan _updatePeriod = TimeSpan.FromSeconds(90);

        public ShowcaseSettingsUpdaterService(
            IMultiFactorApi api,
            IWebHostEnvironment hostEnvironment,
            IOptionsMonitorCache<ShowcaseSettings> cache,
            ILogger<ShowcaseSettingsUpdaterService> logger)
        {
            _multifactorApi = api;
            _hostEnvironment = hostEnvironment;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            using PeriodicTimer timer = new PeriodicTimer(_updatePeriod);

            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    var settings = await _multifactorApi.GetShowcaseSettingsAsync();
                    _cache.TryRemove("ShowcaseSettings");
                    _cache.TryAdd("ShowcaseSettings", settings);
                    await UpdateLogos(settings);

                    _logger.LogInformation("Showcase settings loaded successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load Showcase settings");
                }
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
                try
                {
                    var data = await _multifactorApi.GetShowcaseLogoAsync(fileName);
                    if (data is null)
                    {
                        continue;
                    }

                    File.WriteAllBytes(Path.Combine(localFolder, fileName), data);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Failed to load showcase logo '{fileName}'", fileName);
                }
            }

            var extraFiles = localFileNames.Except(cloudFileNames).ToArray();
            foreach (var filename in extraFiles)
            {
                File.Delete(Path.Combine(localFolder, filename));
            }
        }
    }
}