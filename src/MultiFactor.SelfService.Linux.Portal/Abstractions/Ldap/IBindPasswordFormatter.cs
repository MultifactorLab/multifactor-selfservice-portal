namespace MultiFactor.SelfService.Linux.Portal.Abstractions.Ldap
{
    public interface IBindPasswordFormatter
    {
        string Format(string password);
    }

    public class DefaultBindPasswordFormatter : IBindPasswordFormatter
    {
        public string Format(string password) => password;
    }
}
