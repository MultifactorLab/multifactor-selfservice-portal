namespace MultiFactor.SelfService.Linux.Portal.Core.Authentication.AuthenticationClaims
{
    public interface IClaimsSource
    {
        IReadOnlyDictionary<string, string> GetClaims();
    }
}
