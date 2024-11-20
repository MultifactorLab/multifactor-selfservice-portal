namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ActiveDirectorySettings
    {
        // for model binding
        public ActiveDirectorySettings()
        {
                
        }
        
        // for obsolete mapping
        public ActiveDirectorySettings(string secondFactorGroup, bool useUserPhone, bool useMobileUserPhone, string netBiosName, bool requiresUserPrincipalName)
        {
            SecondFactorGroup = secondFactorGroup;
            UseUserPhone = useUserPhone;
            UseMobileUserPhone = useMobileUserPhone;
            NetBiosName = netBiosName;
            RequiresUserPrincipalName = requiresUserPrincipalName;
        }
        public string[] SecondFactorGroups => SecondFactorGroup?.Split(';') ?? Array.Empty<string>();

        /// old setting with single group. we must split in and use <see cref="SecondFactorGroups"/> 
        private string SecondFactorGroup { get; init; }
        public bool UseUserPhone { get; init; }
        public bool UseMobileUserPhone { get; init; }
        public string NetBiosName { get; init; }
        public bool RequiresUserPrincipalName { get; init; }
        public bool UseUpnAsIdentity { get; init; }
    }
}
