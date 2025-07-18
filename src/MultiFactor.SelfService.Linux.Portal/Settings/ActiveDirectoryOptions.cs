namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ActiveDirectorySettings
    {
        // for model binding
        public ActiveDirectorySettings()
        {

        }

        // for obsolete mapping
        public ActiveDirectorySettings(
            string secondFactorGroup,
            bool useUserPhone,
            bool useMobileUserPhone,
            string netBiosName,
            bool requiresUserPrincipalName,
            string activeDirectoryGroup)
        {
            SecondFactorGroup = secondFactorGroup;
            UseUserPhone = useUserPhone;
            UseMobileUserPhone = useMobileUserPhone;
            NetBiosName = netBiosName;
            RequiresUserPrincipalName = requiresUserPrincipalName;
            ActiveDirectoryGroup = activeDirectoryGroup;
        }
        public string[] SecondFactorGroups => SecondFactorGroup?.Split(';') ?? Array.Empty<string>();

        /// old setting with single group. we must split in and use <see cref="SecondFactorGroups"/>
        private string SecondFactorGroup { get; init; }
        public bool UseUserPhone { get; init; }
        public bool UseMobileUserPhone { get; init; }
        public string NetBiosName { get; init; }
        public bool RequiresUserPrincipalName { get; init; }
        public bool UseUpnAsIdentity { get; init; }
        public string UseAttributeAsIdentity { get; private set; } = string.Empty;

        /// <summary>
        /// Only users from these groups has access to the resource
        /// </summary>
        public string ActiveDirectoryGroup { get; init; }
        public string[] SplittedActiveDirectoryGroups => ActiveDirectoryGroup
            ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
    }
}
