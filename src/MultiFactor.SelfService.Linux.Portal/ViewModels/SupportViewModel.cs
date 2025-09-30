using System;

namespace MultiFactor.SelfService.Linux.Portal.ViewModels
{
    public class SupportViewModel
    {
        public string AdminName { get; }
        public string AdminEmail { get; }
        public string AdminPhone { get; }

        public SupportViewModel(string adminName, string adminEmail, string adminPhone)
        {
            AdminName = adminName;
            AdminEmail = adminEmail;
            AdminPhone = adminPhone;
        }

        public static SupportViewModel EmptyModel()
        {
            return new SupportViewModel(string.Empty, string.Empty, string.Empty);
        }

        public bool IsEmpty()
        {
            return
                string.IsNullOrWhiteSpace(AdminName) && 
                string.IsNullOrWhiteSpace(AdminEmail) && 
                string.IsNullOrWhiteSpace(AdminPhone);
        }
    }
}
