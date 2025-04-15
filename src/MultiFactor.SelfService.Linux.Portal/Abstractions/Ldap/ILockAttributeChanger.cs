namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap;

public interface ILockAttributeChanger
{
    public string AttributeName { get; }
    public Task<bool> ChangeLockAttributeValue(string userName, object value);
}