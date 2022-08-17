using System.Text.RegularExpressions;

namespace MultiFactor.SelfService.Linux.Portal.Core.Http
{
    /// <summary>
    /// Request and response JSON payload logger.
    /// </summary>
    public class JsonPayloadLogger
    {
        private static readonly IReadOnlyList<Regex> _rules = new List<Regex>
        {
            // match 'Key' json key
            new Regex("(?:\"key\":\")(.*?)(?:\")", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            // match secret TOTP link
            new Regex("(?:secret=)(.*?)(?:&)", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };

        private readonly ILogger<JsonPayloadLogger> _logger;

        public JsonPayloadLogger(ILogger<JsonPayloadLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs payload. Before removes sensitive data from it.
        /// </summary>
        /// <param name="payload">JSON payload string.</param>
        /// <param name="logPrefix">Log entry prefix (tag).</param>
        public void LogPayload(string payload, string? logPrefix = null)
        {
            // remove totp key from log
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var safeLog = RemoveSensitiveDataFromLog(payload);
                _logger.LogDebug("{prefix:l}: {safeLog:l}", logPrefix ?? "", safeLog);
            }
        }

        private static string RemoveSensitiveDataFromLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return input ?? "";

            foreach (var reg in _rules)
            {
                var match = reg.Match(input);
                if (match.Success)
                {
                    input = input.Replace(match.Groups[1].Value, "*****");
                }
            }

            return input;
        }
    }
}
