﻿using System.ComponentModel.DataAnnotations;

namespace MultiFactor.SelfService.Linux.Portal.Models
{
    public class ChangeExpiredPasswordModel
    {
        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)]
        [MinLength(7, ErrorMessageResourceName = "Minimum7", ErrorMessageResourceType = typeof(Resources.Validation))]
        public string NewPassword { get; set; }

        [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Resources.Validation))]
        [Compare("NewPassword", ErrorMessageResourceName = "PasswordsDoNotMatch", ErrorMessageResourceType = typeof(Resources.Validation))]
        [DataType(DataType.Password)]
        public string NewPasswordAgain { get; set; }
    }
}