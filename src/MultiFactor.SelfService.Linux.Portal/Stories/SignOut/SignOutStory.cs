using System.Text;
using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi;
using MultiFactor.SelfService.Linux.Portal.Integrations.MultifactorIdpApi.Dto;
using MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignOut;

public class SignOutStory
{
    private readonly SafeHttpContextAccessor _contextAccessor;
    private readonly IMultifactorIdpApi _idpApi;
    private readonly ILogger<SignOutStory> _logger;

    public SignOutStory(SafeHttpContextAccessor contextAccessor, IMultifactorIdpApi idpApi, ILogger<SignOutStory> logger)
    {
        _contextAccessor = contextAccessor;
        _idpApi = idpApi;
        _logger = logger;
    }

    public IActionResult Execute()
    {
        _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
        
        var request = new LogoutRequestDto
        {
            Reason = "logout"
        };
        
        try
        {
            var headers = _contextAccessor.HttpContext.GetRequiredHeaders();
            var response = _idpApi.LogoutAsync(request, headers).GetAwaiter().GetResult();

            if (!response.Success)
            {
                _logger.LogWarning("Logout failed: {Error}", response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
        
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

    public async Task<IActionResult> ExecuteAsync(Dictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        
        _contextAccessor.HttpContext.Response.Cookies.Delete(Constants.COOKIE_NAME);
        
        var request = new LogoutRequestDto
        {
            Reason = "logout"
        };
        
        var response = await _idpApi.LogoutAsync(request, headers);

        if (!response.Success)
        {
            _logger.LogWarning("Logout failed: {Error}", response.ErrorMessage);
        }
        
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
