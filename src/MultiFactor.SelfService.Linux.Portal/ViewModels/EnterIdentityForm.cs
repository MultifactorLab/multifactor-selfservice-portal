using Microsoft.AspNetCore.Mvc;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class EnterIdentityForm
    {
        public string Identity { get; set; } = string.Empty;
        /// <summary>
        /// Correct document URL from browser if we behind nginx or other proxy
        /// </summary>
        [HiddenInput]
        public string MyUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Unlock user without password reset
        /// </summary>
        public bool UnlockUser { get; set; }
    }
}