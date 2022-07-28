using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Authentication;

namespace MultiFactor.SelfService.Linux.Portal.Stories.AuthenticateStory
{
    public class AuthenticateSessionStory
    {
        private readonly ApplicationAuthenticationState _authenticationState;

        public AuthenticateSessionStory(ApplicationAuthenticationState authenticationState)
        {
            _authenticationState = authenticationState ?? throw new ArgumentNullException(nameof(authenticationState));
        }

        public IActionResult Execute(string accessToken)
        {
            _authenticationState.Authenticate(accessToken);

            if (_authenticationState.GetTokenClaims().MustChangePassword)
            {
                return new RedirectToActionResult("ChangePassword", "Home", new { });
            }

            return new LocalRedirectResult("/");
        }
    }
}
