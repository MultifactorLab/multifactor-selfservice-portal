using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Helpers;

public static class AccountFlowHelper
{
    public static bool ShouldAttemptKerberos(PortalSettings portalSettings, HttpRequest request)
    {
        return portalSettings.KerberosSettings.Enabled
               && !request.Cookies.ContainsKey(Constants.KERBEROS_ATTEMPTED_COOKIE);
    }

    public static string GetLoginOrIdentityActionName(PortalSettings portalSettings)
    {
        return portalSettings.PreAuthenticationMethod ? "Identity" : "Login";
    }

    public static object BuildSsoRouteValues(SingleSignOnDto sso)
    {
        return new
        {
            samlSessionId = sso.SamlSessionId,
            oidcSessionId = sso.OidcSessionId
        };
    }

    public static void SetKerberosAttemptedCookie(HttpResponse response, TimeSpan expiresIn)
    {
        response.Cookies.Append(
            Constants.KERBEROS_ATTEMPTED_COOKIE,
            "1",
            new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.Add(expiresIn)
            });
    }

    public static void ClearKerberosAttemptedCookie(HttpResponse response)
    {
        response.Cookies.Delete(Constants.KERBEROS_ATTEMPTED_COOKIE);
    }

    public static bool IsNtlmToken(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader["Negotiate ".Length..];
        try
        {
            var bytes = Convert.FromBase64String(token);
            return bytes.Length >= 7
                   && bytes[0] == 'N' && bytes[1] == 'T' && bytes[2] == 'L'
                   && bytes[3] == 'M' && bytes[4] == 'S' && bytes[5] == 'S'
                   && bytes[6] == 'P';
        }
        catch
        {
            return false;
        }
    }

    public static string? ResolveRedirectUrl(IUrlHelper urlHelper, IActionResult result)
    {
        return result switch
        {
            RedirectResult redirect => redirect.Url,
            RedirectToActionResult actionRedirect =>
                urlHelper.Action(actionRedirect.ActionName, actionRedirect.ControllerName, actionRedirect.RouteValues),
            _ => null
        };
    }
}

