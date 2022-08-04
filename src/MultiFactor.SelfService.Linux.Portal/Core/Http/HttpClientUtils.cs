namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    public static class HttpClientUtils
    {
        public static void AddHeadersIfExist(HttpRequestMessage message, IReadOnlyDictionary<string, string>? headers)
        {
            if (headers != null)
            {
                foreach (var h in headers)
                {
                    message.Headers.Add(h.Key, h.Value);
                }
            }
        }

        public static async Task<string?> TryGetContent(HttpResponseMessage response)
        {
            if (response.Content == null)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
