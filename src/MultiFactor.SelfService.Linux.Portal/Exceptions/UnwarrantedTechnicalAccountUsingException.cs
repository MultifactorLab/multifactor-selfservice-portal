namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    internal class UnwarrantedTechnicalAccountUsingException : Exception
    {
        public UnwarrantedTechnicalAccountUsingException() : base("Trying to login as a technical user") { }

        public UnwarrantedTechnicalAccountUsingException(Exception inner) 
            : base($"Trying to login as a technical user: {inner.Message}", inner) { }
    }
}
