namespace MultiFactor.SelfService.Linux.Portal.Extensions;

public static class HttpContextExtensions
{
    public static Dictionary<string, string> GetRequiredHeaders(this HttpContext httpContext)
    {
        var headers = httpContext.Request.Headers
            .Where(h => ShouldForwardHeader(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

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
    
    public static KeyValuePair<string, string> GetAdConnectorUrlHeader(this HttpContext httpContext, string? myBaseUrl = null)
    {
        string url;
        if (!string.IsNullOrEmpty(myBaseUrl))
        {
            url = myBaseUrl.BuildPostbackUrl();
        }
        else
        {
            var proto = httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault()
                        ?? httpContext.Request.Scheme;

            var host = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                       ?? httpContext.Request.Host.Value;

            var prefix = httpContext.Request.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? "";

            url = $"{proto}://{host}{prefix}".TrimEnd('/');
        }
        
        return new KeyValuePair<string, string>(url, httpContext.Request.Headers["X-Forwarded-Path"]);
    }
}