using Microsoft.AspNetCore.DataProtection;

namespace MultiFactor.SelfService.Linux.Portal.Stories.SignInStory
{
    /// <summary>
    /// Protect sensitive data.
    /// </summary>
    public class DataProtection
    {
        private const string _protectorName = "SSPL.Protector";

        private readonly IDataProtectionProvider _dataProtectionProvider;

        public DataProtection(IDataProtectionProvider dataProtectionProvider)
        {
            _dataProtectionProvider = dataProtectionProvider;
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
