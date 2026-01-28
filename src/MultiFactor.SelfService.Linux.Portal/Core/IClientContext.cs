using System.Net;

namespace MultiFactor.SelfService.Linux.Portal.Core;

public interface IClientContext
{
    IPAddress? ClientIp { get; }

    bool IsProxiedRequest { get; }
}
