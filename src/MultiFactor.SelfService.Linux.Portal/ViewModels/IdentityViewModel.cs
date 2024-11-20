using System.ComponentModel.DataAnnotations;
using MultiFactor.SelfService.Linux.Portal.Attributes;
using Resources;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels;

public class IdentityViewModel
{
    [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
    public string UserName { get; set; }
        
    [RequiredIfNotNull(nameof(AccessToken), ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validation))]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    /// <summary>
    /// Correct document URL from browser if we behind nginx or other proxy
    /// </summary>
    public string MyUrl { get; set; }
        
    public string AccessToken { get; set; }
}