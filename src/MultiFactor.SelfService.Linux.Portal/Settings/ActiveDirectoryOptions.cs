namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ActiveDirectorySettings
    {
        public string SecondFactorGroup { get; private set; }
        public bool UseUserPhone { get; private set; }
        public bool UseMobileUserPhone { get; private set; }
        public string NetBiosName { get; private set; }
    }
}
