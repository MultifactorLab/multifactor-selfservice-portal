using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class YandexKeyViewModel
    {
        public string Link { get; set; }
        public string Key { get; set; }

        [Required(ErrorMessage = "Required")]
        public string Otp { get; set; }
    }
}