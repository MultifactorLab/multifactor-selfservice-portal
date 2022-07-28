using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    public class JsonDataSerializer
    {
        private readonly ILogger<JsonDataSerializer> _logger;

        public JsonDataSerializer(ILogger<JsonDataSerializer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public StringContent Serialize(object data)
        {
            var jsonRequest = JsonSerializer.Serialize(data, SerializerOptions.JsonSerializerOptions);
            WriteRequest(jsonRequest);
            return new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        }

        public async Task<T?> Deserialize<T>(HttpContent content)
        {
            var jsonResponse = await content.ReadAsStringAsync();
            WriteResponse(jsonResponse);
            return JsonSerializer.Deserialize<T>(jsonResponse, SerializerOptions.JsonSerializerOptions);
        }

        private void WriteRequest(string payload)
        {
            WriteLog(payload, $"Request to API");
        }

        private void WriteResponse(string payload)
        {
            WriteLog(payload, "Response from API");
        }

        private void WriteLog(string payload, string prefix)
        {
            // remove totp key from log
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var safeLog = RemoveSensitiveDataFromLog(payload);
                _logger.LogDebug("{prefix:l}: {safeLog:l}", prefix, safeLog);
            }
        }

        private static string RemoveSensitiveDataFromLog(string input)
        {
            //match 'Key' json key
            var regex1 = new Regex("(?:\"key\":\")(.*?)(?:\")", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            //match secret TOTP link
            var regex2 = new Regex("(?:secret=)(.*?)(?:&)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var match = regex1.Match(input);
            if (match.Success)
            {
                input = input.Replace(match.Groups[1].Value, "*****");
            }

            match = regex2.Match(input);
            if (match.Success)
            {
                input = input.Replace(match.Groups[1].Value, "*****");
            }

            return input;
        }
    }
}
