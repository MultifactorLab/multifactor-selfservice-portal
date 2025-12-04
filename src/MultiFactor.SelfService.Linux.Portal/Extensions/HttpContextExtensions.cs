namespace MultiFactor.SelfService.Linux.Portal.Extensions;

public static class HttpContextExtensions
{
    public static Dictionary<string, string> GetRequiredHeaders(this HttpContext httpContext)
    {
        return httpContext.Request.Headers
            .Where(h => ShouldForwardHeader(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToString());
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

}