namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Exceptions
{
    /// <summary>
    /// Indicates that the response payload property "Success" is "false";
    /// </summary>
    [Serializable]
    internal class UnsuccessfulResponseException : Exception
    {
        public UnsuccessfulResponseException() { }
        public UnsuccessfulResponseException(string message) : base(message) { }
        public UnsuccessfulResponseException(string message, Exception inner) : base(message, inner) { }
        protected UnsuccessfulResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
