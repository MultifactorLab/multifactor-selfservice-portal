using Resources;
using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
        public string UserName { get; set; }

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Correct document URL from browser if we behind nginx or other proxy
        /// </summary>
        public string MyUrl { get; set; }
    }
}
