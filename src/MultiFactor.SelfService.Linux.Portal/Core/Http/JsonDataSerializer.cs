using System.Text;
using System.Text.Json;

namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    public class JsonDataSerializer
    {
        private readonly JsonPayloadLogger? _payloadLogger;

        public JsonDataSerializer(JsonPayloadLogger? payloadLogger)
        {
            _payloadLogger = payloadLogger;
        }

        public StringContent Serialize(object data, string? logPrefix = null)
        {
            var jsonRequest = JsonSerializer.Serialize(data, SerializerOptions.JsonSerializerOptions);
            _payloadLogger?.LogPayload(jsonRequest, logPrefix);
            return new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        }

        public async Task<T?> Deserialize<T>(HttpContent content, string? logPrefix = null)
        {
            var jsonResponse = await content.ReadAsStringAsync();
            _payloadLogger?.LogPayload(jsonResponse, logPrefix);
            return JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions.JsonSerializerOptions);
        }
    }
}
