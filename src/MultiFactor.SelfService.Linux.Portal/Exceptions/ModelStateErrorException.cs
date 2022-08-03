namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    [Serializable]
    internal class ModelStateErrorException : Exception
    {
        public string ViewName { get; }

        public ModelStateErrorException(string viewName)
        {
            ViewName = viewName;
        }
        public ModelStateErrorException(string message, string viewName) : base(message)
        {
            ViewName = viewName;
        }
        public ModelStateErrorException(string message, string viewName, Exception inner) : base(message, inner)
        {
            ViewName = viewName;
        }

        protected ModelStateErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
