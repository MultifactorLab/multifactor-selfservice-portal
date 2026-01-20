using System.Net;

namespace MultiFactor.SelfService.Linux.Portal.Core;

internal class ClientContext : IClientContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc />
    public IPAddress? ClientIp
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return null;
            }

            // Primary source: RemoteIpAddress resolved by ForwardedHeadersMiddleware
            return httpContext.Connection.RemoteIpAddress;
        }
    }

    /// <inheritdoc />
    public bool IsProxiedRequest
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return false;
            }

            // Check if forwarding headers were present
            // ForwardedHeadersMiddleware processes X-Forwarded-For, X-Forwarded-Proto, etc.
            return httpContext.Request.Headers.ContainsKey("X-Forwarded-For") ||
                   httpContext.Request.Headers.ContainsKey("X-Real-IP") ||
                   httpContext.Request.Headers.ContainsKey("Forwarded");
        }
    }
}
