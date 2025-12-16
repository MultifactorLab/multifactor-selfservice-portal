namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto
{
    public record UserProfileIdpDto(
        string Id,
        string Identity,
        string Name,
        string Email);
}