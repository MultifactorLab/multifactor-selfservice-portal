using System.Net;

namespace MultiFactor.SelfService.Linux.Portal.Core;

internal class ClientContext : IClientContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    
    public IPAddress? ClientIp
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }
            
            return httpContext.Connection.RemoteIpAddress;
        }
    }
    
    public bool IsProxiedRequest
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return false;
            }
            
            return httpContext.Request.Headers.ContainsKey("X-Forwarded-For") ||
                   httpContext.Request.Headers.ContainsKey("X-Real-IP") ||
                   httpContext.Request.Headers.ContainsKey("Forwarded");
        }
    }
}
