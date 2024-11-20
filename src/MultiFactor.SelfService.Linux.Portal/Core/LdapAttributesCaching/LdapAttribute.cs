namespace MultiFactor.SelfService.Linux.Portal.Core.LdapAttributesCaching
{
    public class LdapAttribute
    {
        public string Name { get; }
        public IReadOnlyList<string> Values { get; }

        private LdapAttribute(string name, IReadOnlyList<string> values)
        {
            Name = name;
            Values = values;
        }

        public static LdapAttribute Create(string name, IEnumerable<string> value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            ArgumentNullException.ThrowIfNull(value);

            return new LdapAttribute(name, value.ToList().AsReadOnly());
        }
    }
}
