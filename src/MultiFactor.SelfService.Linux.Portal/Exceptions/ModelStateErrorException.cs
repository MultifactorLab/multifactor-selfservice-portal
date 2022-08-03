namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    [Serializable]
    internal class ModelStateErrorException : Exception
    {
        public ModelStateErrorException() { }

        public ModelStateErrorException(string message) : base(message) {}

        public ModelStateErrorException(string message, string viewName, Exception inner) : base(message, inner) {}

        protected ModelStateErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
