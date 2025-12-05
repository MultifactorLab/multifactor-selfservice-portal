namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class MultifactorIdpApiSettings
    {
        public const string SectionName = "MultifactorIdpApiSettings";
        
        public string ApiUrl { get; private set; } = string.Empty;
        
        public string SspBaseUrl { get; private set; } = string.Empty;
    }
}
