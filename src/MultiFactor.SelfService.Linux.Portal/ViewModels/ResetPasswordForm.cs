using Microsoft.AspNetCore.Mvc;
using Resources;
using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class ResetPasswordForm
    {
        [HiddenInput]
        [Required]
        public string Identity { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
        [MinLength(1, ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
        [Compare("NewPassword", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(Validation))]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; } = string.Empty;
    }
}
