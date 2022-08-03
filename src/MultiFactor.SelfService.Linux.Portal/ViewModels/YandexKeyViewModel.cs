using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class YandexKeyViewModel
    {
        public string Link { get; set; }
        public string Key { get; set; }

        // TODO
        //[Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Required(ErrorMessage = "Required")]
        public string Otp { get; set; }
    }
}