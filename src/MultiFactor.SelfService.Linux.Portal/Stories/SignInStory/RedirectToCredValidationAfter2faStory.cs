using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core.Caching;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory;

public class RedirectToCredValidationAfter2FaStory
{
    private readonly ILogger<RedirectToCredValidationAfter2FaStory> _logger;
    private readonly IApplicationCache _applicationCache;
    private readonly IMultifactorIdpApi _idpApi;
    private readonly SafeHttpContextAccessor _contextAccessor;

    public RedirectToCredValidationAfter2FaStory(
        IApplicationCache applicationCache,
        ILogger<RedirectToCredValidationAfter2FaStory> logger,
        IMultifactorIdpApi idpApi,
        SafeHttpContextAccessor contextAccessor)
    {
        _logger = logger;
        _applicationCache = applicationCache;
        _idpApi = idpApi;
        _contextAccessor = contextAccessor;
    }
    
    public async Task<ActionResult> ExecuteAsync(string accessToken)
    {
        ArgumentNullException.ThrowIfNull(accessToken);
        _logger.LogDebug("Extracting token information for PreAuthenticationMethod flow");
        
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken token;
        try
        {
            token = handler.ReadJwtToken(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse access token");
            return new RedirectToActionResult("Login", "Account", null);
        }

        var requestId = token.Id;
        if (string.IsNullOrEmpty(requestId))
        {
            _logger.LogError("Token ID is missing");
            return new RedirectToActionResult("Login", "Account", null);
        }
        
        var request = new LoginCompletedRequestDto
        {
            AccessToken = accessToken
        };

        try
        {
            var response = await _idpApi.LoginCompletedAsync(request, _contextAccessor.HttpContext.GetRequiredHeaders());
            
            var username = response.RawUserName ?? response.Identity;
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogError("Cannot determine username from token");
                return new RedirectToActionResult("Login", "Account", null);
            }
            
            _applicationCache.SetIdentity(requestId,
                new IdentityViewModel 
                { 
                    UserName = username, 
                    AccessToken = accessToken 
                });

            object routeValue = new { requestId = requestId };
            
            if (!string.IsNullOrEmpty(response.SamlSessionId))
            {
                _logger.LogDebug("SAML session found, redirecting to Identity with SAML session");
                routeValue = new { samlSessionId = response.SamlSessionId, requestId = requestId };
                return new RedirectToActionResult("Identity", "Account", routeValue);
            }

            if (!string.IsNullOrEmpty(response.OidcSessionId))
            {
                _logger.LogDebug("OIDC session found, redirecting to Identity with OIDC session");
                routeValue = new { oidcSessionId = response.OidcSessionId, requestId = requestId };
                return new RedirectToActionResult("Identity", "Account", routeValue);
            }

            _logger.LogDebug("Redirecting to Identity page for password entry");
            return new RedirectToActionResult("Identity", "Account", routeValue);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to extract token information");
            return new RedirectToActionResult("Login", "Account", null);
        }
    }
}
