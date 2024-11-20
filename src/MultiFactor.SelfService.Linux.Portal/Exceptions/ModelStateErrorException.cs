namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    internal class ModelStateErrorException : Exception
    {
        public ModelStateErrorException() { }

        public ModelStateErrorException(string message) : base(message) {}

        public ModelStateErrorException(string message, string viewName, Exception inner) : base(message, inner) {}
    }
}
