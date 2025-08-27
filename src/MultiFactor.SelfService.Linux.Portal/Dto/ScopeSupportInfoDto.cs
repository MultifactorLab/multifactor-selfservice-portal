using MultiFactor.SelfService.Linux.Portal.ViewModels;

namespace MultiFactor.SelfService.Linux.Portal.Dto;

public class ScopeSupportInfoDto
{
    public string AdminName { get; set; }
    public string AdminEmail { get; set; }
    public string AdminPhone { get; set; }

    public static SupportViewModel ToModel(ScopeSupportInfoDto dto)
    {
        return dto == null 
            ? SupportViewModel.EmptyModel()
            : new SupportViewModel(dto.AdminName, dto.AdminEmail, dto.AdminPhone);
    }
}  