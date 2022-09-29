using MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.Abstractions;

namespace MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding
{
    /// <summary>
    /// LDAP filter representation.
    /// </summary>
    public class LdapFilter : ILdapFilter
    {
        private bool _not = false;

        private readonly string _attribute;
        private readonly string _value;

        private LdapFilter(string attribute, string value)
        {
            if (string.IsNullOrEmpty(attribute))
            {
                throw new ArgumentException($"'{nameof(attribute)}' cannot be null or empty.", nameof(attribute));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"'{nameof(value)}' cannot be null or empty.", nameof(value));
            }

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
        public static ILdapFilter Create(string attribute, string value)
        {
            return new LdapFilter(attribute, value);
        }

        public ILdapFilter And(ILdapFilter filter)
        {
            return LdapFilterGroup.Create(LdapFilterGroup.LdapFilterOperator.And).Add(this, filter);
        }

        public ILdapFilter Or(ILdapFilter filter)
        {
            return LdapFilterGroup.Create(LdapFilterGroup.LdapFilterOperator.Or).Add(this, filter);
        }

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
