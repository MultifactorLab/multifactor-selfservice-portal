namespace MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory.Dto
{
    public record ApplicationInfoDto (string Environment, string TimeStamp, string Version, string ApiStatus, string LdapServicesStatus);
}
