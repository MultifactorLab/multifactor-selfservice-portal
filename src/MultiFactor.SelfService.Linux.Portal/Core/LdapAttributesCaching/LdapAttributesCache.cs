namespace MultiFactor.SelfService.Linux.Portal.Core.LdapAttributesCaching
{
    public class LdapAttributesCache : ILdapAttributesCache
    {
        private readonly List<LdapAttribute> _attributes = new();
        public IReadOnlyList<LdapAttribute> Entries => _attributes.AsReadOnly();

        public string GetValue(string name) =>
            _attributes.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Values.FirstOrDefault();

        public IReadOnlyList<string> GetValues(string name) =>
            _attributes.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Values ?? Array.Empty<string>();

        public void AddAttribute(string attr, IEnumerable<string> values)
        {
            _attributes.Add(LdapAttribute.Create(attr, values));
        }
    }
}
