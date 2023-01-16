using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class MultifactorApiDtoExtensions
    {
        public static UserProfileDto ToUserProfileDto(this UserProfileApiDto apiDto, PortalSettings settings)
        {
            if (apiDto is null) throw new ArgumentNullException(nameof(apiDto));
            if (settings is null) throw new ArgumentNullException(nameof(settings));
            
            return new UserProfileDto
            {
                Id = apiDto.Id,
                Identity = apiDto.Identity,
                Name = apiDto.Name,
                Email = apiDto.Email,

                TotpAuthenticators = apiDto.TotpAuthenticators,
                TelegramAuthenticators = apiDto.TelegramAuthenticators,
                MobileAppAuthenticators = apiDto.MobileAppAuthenticators,
                PhoneAuthenticators = apiDto.PhoneAuthenticators,

                Policy = apiDto.Policy,

                EnablePasswordManagement = settings.EnablePasswordManagement,
                EnableExchangeActiveSyncDevicesManagement = settings.EnableExchangeActiveSyncDevicesManagement
            };
        }
    }
}
