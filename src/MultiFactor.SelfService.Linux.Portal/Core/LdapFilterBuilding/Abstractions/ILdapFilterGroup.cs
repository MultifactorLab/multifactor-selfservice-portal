namespace MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.Abstractions
{
    /// <summary>
    /// Provides methods for building filter group as a part of a complex ldapsearch filter.
    /// </summary>
    public interface ILdapFilterGroup : ILdapFilter
    {
        /// <summary>
        /// Adds new filters to the end of current filter group then returns current filter group.
        /// <code>Representation examples:
        /// (&amp;(one)(two)) -> (&amp;(one)(two)(new))
        /// (|(one)(two)) -> (|(one)(two)(new))
        /// (|(one)(two)) -> (|(one)(two)(new1)(new2))
        /// </code>        
        /// </summary>
        /// <param name="filters">New filters</param>
        /// <returns>Current filter group</returns>
        ILdapFilter Add(params ILdapFilter[] filters);

        /// <summary>
        /// Creates new filter and adds it to the end of current filter group then returns current filter group.
        /// <code>Representation examples:
        /// (&amp;(one)(two)) -> (&amp;(one)(two)(new))
        /// (|(one)(two)) -> (|(one)(two)(new))
        /// (|(one)(two)) -> (|(one)(two)(new1)(new2))
        /// </code>
        /// </summary>
        /// <param name="attribute">Attribute name.</param>
        /// <param name="value">Attribute value.</param>
        /// <returns>Current filter group</returns>
        ILdapFilter Add(string attribute, string value);
    }
}
