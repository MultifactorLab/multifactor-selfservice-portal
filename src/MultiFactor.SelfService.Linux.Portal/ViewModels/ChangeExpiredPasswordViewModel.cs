using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class ChangeExpiredPasswordViewModel
    {
        [Required(ErrorMessage = "Required")]
        [DataType(DataType.Password)]
        [MinLength(7, ErrorMessage = "Minimum7")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Required")]
        [Compare("NewPassword", ErrorMessage = "PasswordsDoNotMatch")]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; }
    }
}
