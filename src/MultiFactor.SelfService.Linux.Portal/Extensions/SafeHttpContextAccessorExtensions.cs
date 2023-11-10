using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Http;
using MultiFactor.SelfService.Linux.Portal.Core.LdapAttributesCaching;
using MultiFactor.SelfService.Linux.Portal.Dto;
using MultiFactor.SelfService.Linux.Portal.Integrations.Ldap.CredentialVerification;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    public static class SafeHttpContextAccessorExtensions
    {
        public static SingleSignOnDto SafeGetSsoClaims(this SafeHttpContextAccessor accessor)
        {
            return accessor.HttpContext.Items[Constants.SsoClaims] as SingleSignOnDto 
                ?? new SingleSignOnDto(string.Empty, string.Empty);
        }

        public static CredentialVerificationResult SafeGetGredVerificationResult(this SafeHttpContextAccessor accessor)
        {
            return accessor.HttpContext.Items[Constants.CredentialVerificationResult] as CredentialVerificationResult 
                ?? CredentialVerificationResult.CreateBuilder(false).Build();
        }
        
        public static ILdapAttributesCache SafeGetLdapAttributes(this SafeHttpContextAccessor accessor)
        {
            return accessor.HttpContext.Items[Constants.LoadedLdapAttributes] as ILdapAttributesCache 
                ?? new LdapAttributesCache();
        }
    }
}
