using System.Text;
using System.Text.Json;

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

        public static StringContent ToJsonContent(object data)
        {
            var json = JsonSerializer.Serialize(data, SerializerOptions.JsonSerializerOptions);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        public static async Task<T?> FromJsonContent<T>(HttpContent content)
        {
            var jsonResponse = await content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions.JsonSerializerOptions);
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
