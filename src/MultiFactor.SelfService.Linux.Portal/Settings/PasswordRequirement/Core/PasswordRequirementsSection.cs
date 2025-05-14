namespace MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement.Core
{
    public class PasswordRequirementsSection
    {
        public List<PasswordRequirementItem> PwdRequirement { get; set; }
    }    
    
    public class PasswordRequirementItem
    {
        public bool Enabled { get; set; }
        public string Condition { get; set; }
        public string Value { get; set; }
        public string DescriptionEn { get; set; }
        public string DescriptionRu { get; set; }
    }
}