using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;

/// <summary>
/// New version of SignOutStory that delegates logout logic to IdP.
/// </summary>
public class SignOutStoryV2
{
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly IMultifactorIdpApi _idpApi;
    private readonly ILogger<SignOutStoryV2> _logger;

    public SignOutStoryV2(SafeHttpContextAccessor contextAccessor, IMultifactorIdpApi idpApi, ILogger<SignOutStoryV2> logger)
    {
        _contextAccessor = contextAccessor;
        _idpApi = idpApi;
        _logger = logger;
    }

    public async Task<IActionResult> ExecuteAsync(Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        // Remove MFA cookie
        _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
        
        // Build logout request
        var request = new LogoutRequestDto
        {
            Reason = "logout"
        };

        // Call IdP logout endpoint
        var response = await _idpApi.LogoutAsync(request, headers);

        if (!response.Success)
        {
            _logger.LogWarning("Logout failed: {Error}", response.ErrorMessage);
        }

        // Build redirect URL with SSO session info
        var redirectUrl = new StringBuilder("/account/login");
        var claimsDto = MultiFactorClaimsDtoBinder.FromRequest(_contextAccessor.HttpContext.Request);
        if (claimsDto.HasSamlSession())
        {
            redirectUrl.Append($"?{Constants.MultiFactorClaims.SamlSessionId}={claimsDto.SamlSessionId}");
        }

        if (claimsDto.HasOidcSession())
        {
            redirectUrl.Append($"?{Constants.MultiFactorClaims.OidcSessionId}={claimsDto.OidcSessionId}");
        }

        var res = redirectUrl.ToString();

        return new RedirectResult(res, false);
    }
}
