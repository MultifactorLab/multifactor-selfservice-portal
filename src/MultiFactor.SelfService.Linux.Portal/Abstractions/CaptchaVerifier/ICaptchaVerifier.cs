namespace MultiFactor.SelfService.Linux.Portal.Abstractions.CaptchaVerifier
{
    public interface ICaptchaVerifier
    {
        Task<CaptchaVerificationResult> VerifyCaptchaAsync(HttpRequest request);
    }
}
