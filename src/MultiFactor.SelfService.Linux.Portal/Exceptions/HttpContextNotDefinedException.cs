namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    internal class HttpContextNotDefinedException : Exception
    {
        public HttpContextNotDefinedException() { }
        public HttpContextNotDefinedException(string message) : base(message) { }
        public HttpContextNotDefinedException(string message, Exception inner) : base(message, inner) { }
    }
}
