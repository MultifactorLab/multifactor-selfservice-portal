namespace MultiFactor.SelfService.Linux.Portal.Exceptions
{
    [Serializable]
    public class FeatureNotEnabledException : Exception
    {
        public string FeatureDescription { get; init; }

        public FeatureNotEnabledException(string featureName)
        { 
            FeatureDescription = featureName;
        }

        public FeatureNotEnabledException(string featureName, string message) : base(message)
        {
            FeatureDescription = featureName;
        }

        public FeatureNotEnabledException(string featureName, string message, Exception innerException) : base(message, innerException)
        {
            FeatureDescription = featureName;
        }
    }
}
