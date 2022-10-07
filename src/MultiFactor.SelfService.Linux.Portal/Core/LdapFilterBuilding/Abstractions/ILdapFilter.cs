namespace MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.Abstractions
{
    /// <summary>
    /// LdapFilter API.
    /// Provides helper methods for building complex ldapsearch filter.
    /// </summary>
    public interface ILdapFilter
    {
        /// <summary>
        /// Creates new filter and grouped it with the current filter using AND operator then returns new filter group.
        /// <code>Representation examples:
        /// (current) -> (&amp;(current)(new))
        /// </code>
        /// </summary>
        /// <param name="filter">New filter.</param>
        /// <returns>New filter group.</returns>
        ILdapFilter And(ILdapFilter filter);

        /// <summary>
        /// Creates new filter and grouped it with the current filter using AND operator then returns new filter group.
        /// <code>Representation examples:
        /// (current) -> (&amp;(current)(new))
        /// </summary>
        /// <param name="attribute">Attribute name</param>
        /// <param name="value">Attribute value.</param>
        /// <returns>New filter group.</returns>
        ILdapFilter And(string attribute, string value);

        /// <summary>
        /// Creates new filter and grouped it with the current filter using OR operator then returns new filter group.
        /// <code>Representation examples:
        /// (current) -> (|(current)(new))
        /// </code>
        /// </summary>
        /// <param name="filter">New filter.</param>
        /// <returns>New filter group.</returns>
        ILdapFilter Or(ILdapFilter filter);

        /// <summary>
        /// Creates new filter and grouped it with the current filter using OR operator then returns new filter group.
        /// <code>Representation examples:
        /// (current) -> (|(current)(new))
        /// </code>
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns>New filter group.</returns>
        ILdapFilter Or(string attribute, string value);

        /// <summary>
        /// Adds negation operator to the current filter and returns current filter.
        /// <code>Representation examples: 
        /// (current) -> (!(current))
        /// </code>
        /// </summary>
        /// <returns>Current filter.</returns>
        ILdapFilter Not();

        /// <summary>
        /// Builds filter and returns filter string representation.
        /// </summary>
        /// <returns>String representation of the current ldapsearch filter.</returns>
        string Build();
    }
}
