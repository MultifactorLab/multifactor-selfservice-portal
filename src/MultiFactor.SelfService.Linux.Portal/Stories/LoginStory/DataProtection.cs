using Microsoft.AspNetCore.DataProtection;

namespace MultiFactor.SelfService.Linux.Portal.Stories.LoginStory
{
    /// <summary>
    /// Protect sensitive data.
    /// </summary>
    public class DataProtection
    {
        private const string _protectorName = "SSPL.Protector";

        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly PortalSettings _settings;

        public DataProtection(IDataProtectionProvider dataProtectionProvider, PortalSettings settings)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _settings = settings;
        }

        public string Protect(string data)
        {
            var protector = _dataProtectionProvider.CreateProtector(_protectorName);
            return protector.Protect(data);
            //_ = ProtectedData.Protect(new byte[] { }, new byte[] { }, DataProtectionScope.LocalMachine);
        }

        public string Unprotect(string data)
        {
            var protector = _dataProtectionProvider.CreateProtector(_protectorName);
            return protector.Unprotect(data);
        }
    } 
}
