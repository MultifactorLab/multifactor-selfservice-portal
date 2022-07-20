namespace MultiFactor.SelfService.Linux.Portal.Services.Api.Dto
{
    /// <summary>
    /// Access token for user within non-mfa group
    /// </summary>
    public record BypassPage (string CallbackUrl, string AccessToken);
}
