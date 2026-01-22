using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;
using MultiFactor.SelfService.Linux.Portal.Options;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoadProfileStory
{
    public class FilterShowcaseLinksStory
    {
        private readonly IShowcaseSettingsOptions _showcaseSettings;

        public FilterShowcaseLinksStory(IShowcaseSettingsOptions showcaseSettings)
        {
            _showcaseSettings = showcaseSettings;
        }

        public IReadOnlyCollection<ShowcaseLink> Execute(UserProfilePolicyDto policy)
        {
            var allLinks = _showcaseSettings.CurrentValue?.Links ?? Array.Empty<ShowcaseLink>();

            if (policy?.AllResourcesPermitted == true)
            {
                return allLinks;
            }

            var permittedResources = policy?.PermittedResources?
                .ToHashSet()
                ?? new HashSet<string>();

            var filtered = allLinks
                .Where(x => !string.IsNullOrWhiteSpace(x.ResourceId) && permittedResources.Contains(x.ResourceId))
                .ToArray();

            return filtered;
        }
    }
}
