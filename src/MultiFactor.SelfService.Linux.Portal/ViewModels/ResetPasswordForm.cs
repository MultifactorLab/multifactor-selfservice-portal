using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class ResetPasswordForm
    {
        [HiddenInput]
        public string Identity { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; } = string.Empty;
    }
}
