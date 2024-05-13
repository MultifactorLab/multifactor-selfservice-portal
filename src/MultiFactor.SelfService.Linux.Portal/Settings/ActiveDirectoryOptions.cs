namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ActiveDirectorySettings
    {
        public string SecondFactorGroup { get; init; }
        public bool UseUserPhone { get; init; }
        public bool UseMobileUserPhone { get; init; }
        public string NetBiosName { get; init; }
        public bool RequiresUserPrincipalName { get; init; }
        public bool UseUpnAsIdentity { get; init; }
    }
}
