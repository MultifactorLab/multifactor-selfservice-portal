using Microsoft.AspNetCore.DataProtection;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory
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
        }

        public string Unprotect(string data)
        {
            var protector = _dataProtectionProvider.CreateProtector(_protectorName);
            return protector.Unprotect(data);
        }
    }
}
