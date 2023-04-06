using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class ResetPasswordForm
    {
        [Required(ErrorMessageResourceName = "SomethingWentWrong", ErrorMessageResourceType = typeof(Resources.Error))]
        [HiddenInput]
        public string Identity { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)]
        [MinLength(7, ErrorMessageResourceName = "Minimum7", ErrorMessageResourceType = typeof(Resources.Validation))]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Compare("NewPassword", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; } = string.Empty;
    }
}
