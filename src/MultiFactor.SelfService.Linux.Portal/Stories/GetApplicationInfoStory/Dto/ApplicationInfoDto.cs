namespace MultiFactor.SelfService.Linux.Portal.Stories.GetApplicationInfoStory.Dto
{
    public record ApplicationInfoDto (string TimeStamp, string version, string ApiStatus, string LdapServicesStatus);
}
