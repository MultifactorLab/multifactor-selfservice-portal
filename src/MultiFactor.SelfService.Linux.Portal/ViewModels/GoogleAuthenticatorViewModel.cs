using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class GoogleAuthenticatorViewModel
    {
        public string Link { get; set; }
        public string Key { get; set; }

        //[Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Required(ErrorMessage = "Required")]
        public string Otp { get; set; }
    }
}