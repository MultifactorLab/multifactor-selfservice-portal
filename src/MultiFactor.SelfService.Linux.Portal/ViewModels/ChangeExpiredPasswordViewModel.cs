using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class ChangeExpiredPasswordViewModel
    {
        //[Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Required(ErrorMessage = "Required")]
        [DataType(DataType.Password)]
        //[MinLength(7, ErrorMessageResourceName = "Minimum7", ErrorMessageResourceType = typeof(Resources.Validation))]
        [MinLength(7, ErrorMessage = "Minimum7")]
        public string NewPassword { get; set; }

        //[Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Required(ErrorMessage = "Required")]
        //[Compare("NewPassword", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Compare("NewPassword", ErrorMessage = "PasswordsDoNotMatch")]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; }
    }
}
