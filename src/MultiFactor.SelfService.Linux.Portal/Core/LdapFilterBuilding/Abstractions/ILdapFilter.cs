namespace MultiFactor.SelfService.Linux.Portal.Core.LdapFilterBuilding.Abstractions
{
    /// <summary>
    /// Provides methods for the LDAP filter building.
    /// </summary>
    public interface ILdapFilter
    {
        /// <summary>
        /// Creates new filter and grouped it with the current filter using AND operator then returns new filter group.
        /// <code>Representation examples:
        /// (current) -> (&(current)(new))
        /// </code>
        /// </summary>
        /// <param name="filter">New filter.</param>
        /// <returns>New filter group.</returns>
        ILdapFilter And(ILdapFilter filter);

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
        /// Adds negation operator to the current filter and returns current filter.
        /// <code>Representation examples: 
        /// (current) -> (!(current))
        /// </code>
        /// </summary>
        /// <returns>Current filter.</returns>
        ILdapFilter Not();

        /// <summary>
        /// Builds filter and returns its string representation.
        /// </summary>
        /// <returns>String representation of the current filter.</returns>
        string Build();
    }
}
