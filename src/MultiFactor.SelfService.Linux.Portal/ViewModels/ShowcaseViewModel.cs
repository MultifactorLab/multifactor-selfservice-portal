using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class ShowcaseViewModel
    {
        public UserProfileDto Profile { get; init; }

        public IReadOnlyCollection<ShowcaseLink> ShowcaseLinks { get; init; }
    }
}
