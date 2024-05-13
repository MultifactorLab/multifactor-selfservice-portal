using MultiFactor.SelfService.Linux.Portal.Extensions;

namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    public static class HttpClientUtils
    {
        public static void AddHeadersIfExist(HttpRequestMessage message, IReadOnlyDictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var h in headers)
                {
                    message.Headers.Add(h.Key, h.Value);
                }
            }
        }

        public static async Task<string> TryGetContent(HttpResponseMessage response)
        {
            if (response.Content == null)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }
        
        public static string GetRequestIp(HttpContext context, HttpHeaderAccessor headerAccessor, bool tryRealIp = true, bool tryUseXForwardHeader = true)
        {
            string ip = string.Empty;
            
            if (tryRealIp)
                ip = headerAccessor.GetHeaderValueAs<string>("X-Real-IP")?.SplitCsv()?.FirstOrDefault() ?? string.Empty;
            
            if (tryUseXForwardHeader)
                ip = headerAccessor.GetHeaderValueAs<string>("X-Forwarded-For")?.SplitCsv()?.FirstOrDefault() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(ip) && context?.Connection?.RemoteIpAddress != null)
                ip = context.Connection.RemoteIpAddress.ToString();

            if (string.IsNullOrWhiteSpace(ip))
                ip = headerAccessor.GetHeaderValueAs<string>("REMOTE_ADDR") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(ip))
                throw new Exception("Unable to determine caller's IP.");

            return ip;
        }
    }
}
