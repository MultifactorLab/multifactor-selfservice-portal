using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.Abstractions;

namespace MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding
{
    /// <summary>
    /// API for building ldapsearch filter.
    /// </summary>
    public class LdapFilter : ILdapFilter
    {
        private bool _not;

        private readonly string _attribute;
        private readonly string _value;

        private LdapFilter(string attribute, string value)
        {
            _attribute = attribute;
            _value = value;
        }

        /// <summary>
        /// Returns new filter with '=' operator.
        /// <code>Representation examples:
        /// (<paramref name="attribute"/>=<paramref name="value"/>)
        /// </code>
        /// </summary>
        /// <param name="attribute">Attribute name.</param>
        /// <param name="value">Attribute value.</param>
        /// <returns>New LDAP filter.</returns>
        public static ILdapFilter Create(string attribute, string value)
        {
            if (string.IsNullOrEmpty(attribute))
            {
                throw new ArgumentException($"'{nameof(attribute)}' cannot be null or empty.", nameof(attribute));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"'{nameof(value)}' cannot be null or empty.", nameof(value));
            }

            return new LdapFilter(attribute, value);
        }

        /// <summary>
        /// Returns new filter group with specified multiple values.
        /// </summary>
        /// <code>Representation examples:
        /// (|(<paramref name="attribute"/>=value1)(<paramref name="attribute"/>=value2))
        /// </code>
        /// <param name="attribute">Attribute name.</param>
        /// <param name="values">Possible attribute values.</param>
        /// <returns>New LDAP filter group.</returns>
        public static ILdapFilter Create(string attribute, params string[] values)
        {
            if (string.IsNullOrEmpty(attribute))
                throw new ArgumentException($"'{nameof(attribute)}' cannot be null or empty.", nameof(attribute));
            ArgumentNullException.ThrowIfNull(values);
            if (values.Length == 0) throw new ArgumentException($"'nameof(values)' collection cannot be empty.");

            var group = LdapFilterGroup.Create(LdapFilterGroup.LdapFilterOperator.Or);
            foreach (var value in values)
            {
                group.Add(Create(attribute, value));
            }

            return group;
        }

        public ILdapFilter And(ILdapFilter filter) =>
            LdapFilterGroup.Create(LdapFilterGroup.LdapFilterOperator.And).Add(this, filter);

        public ILdapFilter And(string attribute, string value) => And(Create(attribute, value));

        public ILdapFilter Or(ILdapFilter filter) =>
            LdapFilterGroup.Create(LdapFilterGroup.LdapFilterOperator.Or).Add(this, filter);

        public ILdapFilter Or(string attribute, string value) => Or(Create(attribute, value));

        public ILdapFilter Not()
        {
            _not = true;
            return this;
        }

        public string Build()
        {
            var f = $"({_attribute}={_value})";
            return !_not ? f : $"(!{f})";
        }

        public override string ToString()
        {
            return Build();
        }
    }
}