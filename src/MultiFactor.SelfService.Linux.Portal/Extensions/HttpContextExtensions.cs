using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Extensions;

public static class HttpContextExtensions
{
    public static Dictionary<string, string> GetRequiredHeaders(this HttpContext httpContext, PortalSettings settings = null)
    {
        var headers = httpContext.Request.Headers
            .Where(h => ShouldForwardHeader(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        // Добавляем заголовок X-Callback-Base-Url с базовым URL SSP
        var callbackBaseUrl = GetCallbackBaseUrl(httpContext, settings);
        if (!string.IsNullOrWhiteSpace(callbackBaseUrl))
        {
            headers["X-Callback-Base-Url"] = callbackBaseUrl;
        }

        return headers;
    }
    
    private static bool ShouldForwardHeader(string key)
    {
        return RequiredHeaders.Contains(key);
    }
    
    private static readonly HashSet<string> RequiredHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "User-Agent",
        "X-Device-Id",
        "X-Device-Type",
        "X-Real-IP",
        "X-Forwarded-For",
        "Forwarded"
    };
    
    private static string GetCallbackBaseUrl(HttpContext httpContext, PortalSettings settings)
    {
        if (settings?.MultifactorIdpApiSettings?.SspBaseUrl != null)
        {
            var configuredUrl = settings.MultifactorIdpApiSettings.SspBaseUrl.Trim();
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return configuredUrl.TrimEnd('/');
            }
        }
        
        var proto = httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault()
                    ?? httpContext.Request.Scheme;

        var host = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                   ?? httpContext.Request.Host.Value;

        var prefix = httpContext.Request.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? "";

        return $"{proto}://{host}{prefix}".TrimEnd('/');
    }
}