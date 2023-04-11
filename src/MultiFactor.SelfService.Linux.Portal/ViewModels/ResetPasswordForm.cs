using Microsoft.AspNetCore.Mvc;
using Resources;
using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class ResetPasswordForm
    {
        [HiddenInput]
        public string Identity { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
        [MinLength(7, ErrorMessageResourceName = "Minimum7", ErrorMessageResourceType = typeof(Validation))]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
        [Compare("NewPassword", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(Validation))]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; } = string.Empty;
    }
}
