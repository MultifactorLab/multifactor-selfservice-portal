using MultiFactor.SelfService.Linux.Portal.Core;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Dto
{
    public record SingleSignOnDto (string SamlSessionId, string OidcSessionId)
    {
        public bool HasSamlSession() => !string.IsNullOrEmpty(SamlSessionId);
        public bool HasOidcSession() => !string.IsNullOrEmpty(OidcSessionId);

        public override string ToString()
        {
            var sb = new StringBuilder(string.Empty);
            if (HasSamlSession()) sb.Append($"?{Constants.MultiFactorClaims.SamlSessionId}={SamlSessionId}");
            if (HasOidcSession()) sb.Append($"?{Constants.MultiFactorClaims.SamlSessionId}={SamlSessionId}");
            return sb.ToString();
        }
    }
}
