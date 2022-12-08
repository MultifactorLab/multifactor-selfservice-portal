namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class AdditionalClaims
    {
        [ConfigurationKeyName(nameof(Claim))]
        public List<Claim> Claims { get; private set; } = new List<Claim>();

        public bool Log { get; private set; }
    }

    public class Claim
    {
        public string? Name { get; internal set; }

        public string? Value { get; internal set; }

        public string? From { get; internal set; }

        public string? When { get; internal set; }
    }
}
