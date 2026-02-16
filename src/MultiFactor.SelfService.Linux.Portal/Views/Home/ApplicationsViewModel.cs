using MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels;

public class ApplicationItemViewModel
{
    public string Title { get; set; }
    public string Logo { get; set; }
    public string Url { get; set; }
    public bool OpenInNewTab { get; set; } = false;
}

public class ApplicationsPageViewModel
{
    public UserProfileDto UserProfile { get; set; }
    public List<ApplicationItemViewModel> Applications { get; set; }
}
