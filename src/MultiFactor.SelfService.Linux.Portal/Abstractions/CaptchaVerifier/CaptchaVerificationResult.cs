namespace MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier
{
    public record CaptchaVerificationResult(bool Success, string? Message = null);
}
