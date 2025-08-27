using Moq;
using Microsoft.Extensions.Logging;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using MultiFactor.SelfService.Linux.Portal.Services;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Tests
{
    public class ScopeInfoServiceTests
    {
        private readonly Mock<IMultiFactorApi> _mockApiClient;
        private readonly Mock<IApplicationCache> _mockCache;
        private readonly Mock<ILogger<ScopeInfoService>> _mockLogger;
        private readonly ScopeInfoService _service;

        public ScopeInfoServiceTests()
        {
            _mockApiClient = new Mock<IMultiFactorApi>();
            _mockCache = new Mock<IApplicationCache>();
            _mockLogger = new Mock<ILogger<ScopeInfoService>>();
            
            _service = new ScopeInfoService(
                _mockApiClient.Object,
                _mockCache.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetSupportInfo_CacheIsNotEmpty_ShouldReturnCachedValue()
        {
            // Arrange
            var expectedSupportInfo = new SupportViewModel("Admin Name", "admin@example.com", "+1234567890");
            var cachedItem = new CachedItem<SupportViewModel>(expectedSupportInfo);
            
            _mockCache.Setup(x => x.GetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY))
                     .Returns(cachedItem);

            // Act
            var result = await _service.GetSupportInfo();

            // Assert
            Assert.Equal(expectedSupportInfo, result);
            Assert.Equal(expectedSupportInfo.AdminName, result.AdminName);
            Assert.Equal(expectedSupportInfo.AdminEmail, result.AdminEmail);
            Assert.Equal(expectedSupportInfo.AdminPhone, result.AdminPhone);
            
            _mockApiClient.Verify(x => x.GetScopeSupportInfo(), Times.Never);
            _mockCache.Verify(x => x.SetSupportInfo(It.IsAny<string>(), It.IsAny<SupportViewModel>()), Times.Never);
        }

        [Fact]
        public async Task GetSupportInfo_CacheIsEmpty_ShouldCallApiAndCacheResult()
        {
            // Arrange
            var apiResponse = new ScopeSupportInfoDto
            {
                AdminName = "Test Admin",
                AdminEmail = "test@example.com",
                AdminPhone = "+1234567890"
            };
            var emptyCachedItem = CachedItem<SupportViewModel>.Empty;
            
            _mockCache.Setup(x => x.GetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY))
                     .Returns(emptyCachedItem);
            _mockApiClient.Setup(x => x.GetScopeSupportInfo())
                         .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetSupportInfo();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(apiResponse.AdminName, result.AdminName);
            Assert.Equal(apiResponse.AdminEmail, result.AdminEmail);
            Assert.Equal(apiResponse.AdminPhone, result.AdminPhone);
            
            _mockApiClient.Verify(x => x.GetScopeSupportInfo(), Times.Once);
            _mockCache.Verify(x => x.SetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY, 
                It.Is<SupportViewModel>(s => s.AdminName == apiResponse.AdminName)), Times.Once);
        }

        [Theory]
        [InlineData("Admin", "admin@test.com", "+123456")]
        [InlineData("", "", "")]
        [InlineData(null, null, null)]
        public async Task GetSupportInfo_ApiReturnsValidData_ShouldReturnCorrectViewModel(
            string adminName, string adminEmail, string adminPhone)
        {
            // Arrange
            var apiResponse = new ScopeSupportInfoDto
            {
                AdminName = adminName,
                AdminEmail = adminEmail,
                AdminPhone = adminPhone
            };
            var emptyCachedItem = CachedItem<SupportViewModel>.Empty;
            
            _mockCache.Setup(x => x.GetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY))
                     .Returns(emptyCachedItem);
            _mockApiClient.Setup(x => x.GetScopeSupportInfo())
                         .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetSupportInfo();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(adminName, result.AdminName);
            Assert.Equal(adminEmail, result.AdminEmail);
            Assert.Equal(adminPhone, result.AdminPhone);
        }

        [Fact]
        public async Task GetSupportInfo_ApiReturnsNull_ShouldReturnEmptyModel()
        {
            // Arrange
            var emptyCachedItem = CachedItem<SupportViewModel>.Empty;
            
            _mockCache.Setup(x => x.GetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY))
                     .Returns(emptyCachedItem);
            _mockApiClient.Setup(x => x.GetScopeSupportInfo())
                         .ReturnsAsync((ScopeSupportInfoDto)null);

            // Act
            var result = await _service.GetSupportInfo();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsEmpty());
            Assert.Equal(string.Empty, result.AdminName);
            Assert.Equal(string.Empty, result.AdminEmail);
            Assert.Equal(string.Empty, result.AdminPhone);
            
            _mockCache.Verify(x => x.SetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY, 
                It.Is<SupportViewModel>(s => s.IsEmpty())), Times.Once);
        }

        [Fact]
        public async Task GetSupportInfo_ApiThrowsException_ShouldReturnEmptyModelAndLogWarning()
        {
            // Arrange
            var emptyCachedItem = CachedItem<SupportViewModel>.Empty;
            var expectedException = new HttpRequestException("API is unavailable");
            
            _mockCache.Setup(x => x.GetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY))
                     .Returns(emptyCachedItem);
            _mockApiClient.Setup(x => x.GetScopeSupportInfo())
                         .ThrowsAsync(expectedException);

            // Act
            var result = await _service.GetSupportInfo();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsEmpty());
            Assert.Equal(string.Empty, result.AdminName);
            Assert.Equal(string.Empty, result.AdminEmail);
            Assert.Equal(string.Empty, result.AdminPhone);
            
            _mockCache.Verify(x => x.SetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY, 
                It.Is<SupportViewModel>(s => s.IsEmpty())), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to load admin info")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSupportInfo_CacheHasEmptyValue_ShouldReturnEmptyValueDirectly()
        {
            // Arrange
            var emptyModel = SupportViewModel.EmptyModel();
            var cachedItem = new CachedItem<SupportViewModel>(emptyModel);
            
            _mockCache.Setup(x => x.GetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY))
                     .Returns(cachedItem);

            // Act
            var result = await _service.GetSupportInfo();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsEmpty());
            
            _mockApiClient.Verify(x => x.GetScopeSupportInfo(), Times.Never);
            _mockCache.Verify(x => x.SetSupportInfo(It.IsAny<string>(), It.IsAny<SupportViewModel>()), Times.Never);
        }

        [Fact]
        public async Task GetSupportInfo_ShouldUseCorrectCacheKey()
        {
            // Arrange
            var emptyCachedItem = CachedItem<SupportViewModel>.Empty;
            
            _mockCache.Setup(x => x.GetSupportInfo(It.IsAny<string>()))
                     .Returns(emptyCachedItem);
            _mockApiClient.Setup(x => x.GetScopeSupportInfo())
                         .ReturnsAsync(new ScopeSupportInfoDto());

            // Act
            await _service.GetSupportInfo();

            // Assert
            _mockCache.Verify(x => x.GetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY), Times.Once);
            _mockCache.Verify(x => x.SetSupportInfo(Constants.SupportInfo.SUPPORT_INFO_CACHE_KEY, 
                It.IsAny<SupportViewModel>()), Times.Once);
        }
    }
}
