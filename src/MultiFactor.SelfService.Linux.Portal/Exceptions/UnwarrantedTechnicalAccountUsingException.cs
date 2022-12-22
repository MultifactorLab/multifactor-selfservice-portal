namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    [Serializable]
    internal class UnwarrantedTechnicalAccountUsingException : Exception
    {
        public UnwarrantedTechnicalAccountUsingException() : base("Trying to login as a technical user") { }

        public UnwarrantedTechnicalAccountUsingException(Exception inner) 
            : base($"Trying to login as a technical user: {inner.Message}", inner) { }

        protected UnwarrantedTechnicalAccountUsingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
