namespace MultiFactor.SelfService.Linux.Portal.Core.Metadata.GlobalValues
{
    public static class ApplicationGlobalValuesMetadata
    {
        private static readonly string[] _reservedValues;

        static ApplicationGlobalValuesMetadata()
        {
            _reservedValues = Enum.GetNames(typeof(ApplicationGlobalValue));
        }

        public static bool HasKey(string value) => _reservedValues.Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
        public static ApplicationGlobalValue ParseKey(string value) => Enum.Parse<ApplicationGlobalValue>(value, true);
    }
}
