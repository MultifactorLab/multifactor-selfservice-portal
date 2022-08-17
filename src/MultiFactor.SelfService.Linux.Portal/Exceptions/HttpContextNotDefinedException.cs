namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    [Serializable]
    internal class HttpContextNotDefinedException : Exception
    {
        public HttpContextNotDefinedException() { }
        public HttpContextNotDefinedException(string message) : base(message) { }
        public HttpContextNotDefinedException(string message, Exception inner) : base(message, inner) { }
        protected HttpContextNotDefinedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
