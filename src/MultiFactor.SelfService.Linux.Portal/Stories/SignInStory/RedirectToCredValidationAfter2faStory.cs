using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;

public class RedirectToCredValidationAfter2faStory
{
    private readonly ILogger<SignInStory> _logger;
    private readonly ApplicationCache _applicationCache;

    public RedirectToCredValidationAfter2faStory(
        ApplicationCache applicationCache,
        ILogger<SignInStory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _applicationCache = applicationCache ?? throw new ArgumentNullException(nameof(applicationCache));
    }
    
    /*
     * Now we know: username, the fact of successful confirmation of the 2fa and some info about user.
     * Next step - enter password and verify user creds.
     * For this we must correctly pass all known information using the cache and query params.
     */
    public ActionResult Execute(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var usernameClaims = token.Claims.FirstOrDefault(claim => claim.Type == Constants.MultiFactorClaims.RawUserName);

        // for the password entry step
        var requestId = token.Id;
        _applicationCache.SetIdentity(requestId,
            new IdentityViewModel { UserName = usernameClaims?.Value, AccessToken = accessToken });

        object routeValue = new { requestId = requestId };

        #region Process SSO session (if present)

        var oidcClaims = token.Claims.FirstOrDefault(claim => claim.Type == Constants.MultiFactorClaims.OidcSessionId);
        var samlClaims = token.Claims.FirstOrDefault(claim => claim.Type == Constants.MultiFactorClaims.SamlSessionId);
        if (!string.IsNullOrEmpty(samlClaims?.Value))
        {
            routeValue = new { samlSessionId = samlClaims?.Value, requestId = requestId };
            return new RedirectToActionResult("Identity", "Account", routeValue);
        }

        if (!string.IsNullOrEmpty(oidcClaims?.Value))
        {
            routeValue = new { oidcSessionId = oidcClaims?.Value, requestId = requestId };
            return new RedirectToActionResult("Identity", "Account", routeValue);
        }

        #endregion

        return new RedirectToActionResult("Identity", "Account", routeValue);
    }
}