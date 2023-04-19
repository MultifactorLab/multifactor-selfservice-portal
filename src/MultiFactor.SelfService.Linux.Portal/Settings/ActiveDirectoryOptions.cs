namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ActiveDirectorySettings
    {
        public string? SecondFactorGroup { get; private set; }
        public bool UseUserPhone { get; private set; }
        public bool UseMobileUserPhone { get; private set; }
        public string? NetBiosName { get; private set; }
        public bool RequiresUserPrincipalName { get; private set; }

        public ActiveDirectorySettings() { }
        public ActiveDirectorySettings(
            string? secondFactorGroup,
            bool useUserPhone, 
            bool useMobileUserPhone, 
            string? netBiosName,
            bool requiresUserPrincipalName)
        {
            SecondFactorGroup = secondFactorGroup;
            UseUserPhone = useUserPhone;
            UseMobileUserPhone = useMobileUserPhone;
            NetBiosName = netBiosName;
            RequiresUserPrincipalName = requiresUserPrincipalName;
        }
    }
}
