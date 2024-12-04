namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public class UserProfileDto
    {
        public string Id { get; }
        public string Identity { get; }
        public string Name { get; init; }
        public string Email { get; init; }
        
        public bool EnablePasswordManagement { get; init; }
        public bool EnableExchangeActiveSyncDevicesManagement { get; init; }

        public UserProfileDto(string id, string identity)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }
}