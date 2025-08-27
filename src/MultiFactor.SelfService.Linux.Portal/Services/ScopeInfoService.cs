using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Services
{
    public class ScopeInfoService
    {
        private readonly MultiFactorApi _apiClient;
        private readonly ApplicationCache _cache;
        private readonly ILogger<ScopeInfoService> _logger;
        
        public ScopeInfoService(
            MultiFactorApi apiClient, 
            ApplicationCache cache, 
            ILogger<ScopeInfoService> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<SupportViewModel> GetSupportInfo()
        {
            var cachedInfo = _cache.GetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY);
            if (!cachedInfo.IsEmpty)
            {
                return cachedInfo.Value;
            }
            
            return await LoadAndCacheScopeInfo();
        }
        
        private async Task<SupportViewModel> LoadAndCacheScopeInfo()
        {
            try
            {
                var apiResponse = await _apiClient.GetScopeSupportInfo();
                var supportInfo = ScopeSupportInfoDto.ToModel(apiResponse);
                _cache.SetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY, supportInfo);
                return supportInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load admin info: {Message}", ex.Message);
                var emptyModel = SupportViewModel.EmptyModel();
                _cache.SetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY, emptyModel);
                return emptyModel;
            }
        }
    }
}
