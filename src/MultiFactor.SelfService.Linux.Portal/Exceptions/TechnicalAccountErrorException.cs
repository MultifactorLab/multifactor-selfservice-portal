namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    internal class TechnicalAccountErrorException : Exception
    {
        public TechnicalAccountErrorException(string user, string domain) 
            : base($"Unable to bind technical user '{user}' at domain '{domain}'") { }

        public TechnicalAccountErrorException(string user, string domain, Exception inner) 
            : base($"Unable to bind technical user '{user}' at domain '{domain}': {inner.Message}", inner) { }
    }
}
