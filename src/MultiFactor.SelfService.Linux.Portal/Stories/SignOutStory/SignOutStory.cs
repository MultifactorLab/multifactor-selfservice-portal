using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory
{
    public class SignOutStory
    {
        private readonly ApplicationAuthenticationState _authenticationState;

        public SignOutStory(ApplicationAuthenticationState authenticationState)
        {
            _authenticationState = authenticationState ?? throw new ArgumentNullException(nameof(authenticationState));
        }

        public IActionResult Execute(MultiFactorClaimsDto claimsDto)
        {
            _authenticationState.SignOut();

            var redirectUrl = new StringBuilder("/account/login");
            if (claimsDto.HasSamlSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.SamlSessionId}={claimsDto.SamlSession}");
            }

            if (claimsDto.HasOidcSession())
            {
                redirectUrl.Append($"?{MultiFactorClaims.OidcSessionId}={claimsDto.OidcSession}");
            }

            return new RedirectResult(redirectUrl.ToString(), false);
        }
    }
}
