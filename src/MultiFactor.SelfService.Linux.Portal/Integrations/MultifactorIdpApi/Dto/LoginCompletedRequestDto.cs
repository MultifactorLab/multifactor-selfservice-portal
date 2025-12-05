namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;

public sealed class LoginCompletedRequestDto
{
    public required string AccessToken { get; init; }
}

