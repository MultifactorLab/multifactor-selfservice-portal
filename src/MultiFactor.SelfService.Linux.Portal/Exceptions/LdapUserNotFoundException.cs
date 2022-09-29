namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    [Serializable]
    internal class LdapUserNotFoundException : Exception
    {
        public LdapUserNotFoundException() { }

        public LdapUserNotFoundException(string message) : base(message) { }

        public LdapUserNotFoundException(string message, string viewName, Exception inner) : base(message, inner) { }

        protected LdapUserNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
