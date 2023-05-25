using Microsoft.AspNetCore.DataProtection;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory
{
    /// <summary>
    /// Protect sensitive data.
    /// </summary>
    public class DataProtection
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;

        public DataProtection(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtectionProvider = dataProtectionProvider;
        }

        public string Protect(string data, string protectorName)
        {
            var protector = _dataProtectionProvider.CreateProtector(protectorName);
            return protector.Protect(data);
        }

        public string Unprotect(string data, string protectorName)
        {
            var protector = _dataProtectionProvider.CreateProtector(protectorName);
            return protector.Unprotect(data);
        }
    }
}
