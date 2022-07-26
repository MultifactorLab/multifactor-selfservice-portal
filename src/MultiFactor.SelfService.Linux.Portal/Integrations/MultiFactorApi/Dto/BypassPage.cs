namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;

/// <summary>
/// Access token for user within non-mfa group
/// </summary>
public record BypassPageDto(string CallbackUrl, string AccessToken);