using System.Text.Json.Serialization;

namespace MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory.Dto
{
    public class ApplicationInfoDto {
        public string TimeStamp { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Environment { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Version { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ApiStatus { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LdapServicesStatus { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; init; }

        public ApplicationInfoDto(string timeStamp)
        {
            TimeStamp = timeStamp ?? throw new ArgumentNullException(nameof(timeStamp));
        }
    }
}
