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

        /// <summary>
        /// The legitimate user's bind may fail (e.g. expired password).
        /// However, sometimes we STILL need to get some information from the LDAP-directory.
        /// So we'll get it from under the technical account
        /// </summary>
        /// <returns></returns>
        public bool NeedPrebindInfo()
        {
            return UseUpnAsIdentity || string.IsNullOrEmpty(SecondFactorGroup);
        }
    }
}
