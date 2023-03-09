namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class AdditionalClaims
    {
        [ConfigurationKeyName(nameof(Claim))]
        public List<Claim> Claims { get; set; } = new List<Claim>();

        [ConfigurationKeyName(nameof(Claim))] 
        private Claim SingleClaim { get; set; } = new Claim();
        
        public bool Log { get; private set; }
        
        public void EnsureArraysBinging()
        {
            // To deal with a single element binding to array issue, we should map a single claim manually 
            // See: https://github.com/dotnet/runtime/issues/57325
            if (!Claims.Any() &&
                SingleClaim.Value != null)
            {
                Claims.Add(SingleClaim);
            }
        }
    }

    public class Claim
    {
        public string? Name { get; internal set; }

        public string? Value { get; internal set; }

        public string? From { get; internal set; }

        public string? When { get; internal set; }
    }
}
