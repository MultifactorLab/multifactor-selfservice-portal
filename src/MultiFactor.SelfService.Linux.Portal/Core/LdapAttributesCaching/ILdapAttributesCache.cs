namespace MultiFactor.SelfService.Linux.Portal.Core.LdapAttributesCaching
{
    public interface ILdapAttributesCache
    {
        IReadOnlyList<LdapAttribute> Entries { get; }
        string? GetValue(string name);
        IReadOnlyList<string> GetValues(string name);
    }
}
