namespace MultiFactor.SelfService.Linux.Portal.Stories.LoginStory
{
    /// <summary>
    /// Protect sensitive data.
    /// </summary>
    public class DataProtection
    {
        private readonly PortalSettings _settings;

        public DataProtection(PortalSettings settings)
        {
            _settings = settings;
        }

        public string Protect(string data)
        {
            throw new NotImplementedException();
        }

        public string Unprotect(string data)
        {
            throw new NotImplementedException();
        }
    } 
}
