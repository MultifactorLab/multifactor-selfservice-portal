namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public record UserProfileApiDto(
        string Id,
        string Identity,
        string Name,
        string Email,
        UserProfilePolicyApiDto Policy);

    public record UserProfilePolicyApiDto(
        bool AllResourcesPermitted,
        IEnumerable<string> PermittedResources);
}