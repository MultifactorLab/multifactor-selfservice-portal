using System.Text.Json;

namespace MultiFactor.SelfService.Linux.Portal.Core
{
    public static class SerializerOptions
    {
        public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };
    }
}
