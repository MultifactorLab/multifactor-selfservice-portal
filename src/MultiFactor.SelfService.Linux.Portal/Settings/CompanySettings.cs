namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class CompanySettings
    {
        public string Name { get; private set; }
        public string Domain { get; private set; }
        public string LogoUrl { get; private set; }
        public string NestedGroupsBaseDn { get; init; }
        public string[] SplittedNestedGroupsDomain => NestedGroupsBaseDn
            ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
        
        public bool LoadActiveDirectoryNestedGroups { get; private set; } = false;

        public CompanySettings()
        {
        }

        public CompanySettings(
            string name,
            string domain,
            string logoUrl,
            string nestedGroupsBaseDn,
            bool loadActiveDirectoryNestedGroups)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(domain);
            ArgumentException.ThrowIfNullOrWhiteSpace(logoUrl);
            
            (Name, Domain, LogoUrl, NestedGroupsBaseDn, LoadActiveDirectoryNestedGroups) = (name, domain, logoUrl, nestedGroupsBaseDn, loadActiveDirectoryNestedGroups);
        }
    }
}
