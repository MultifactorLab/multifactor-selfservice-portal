namespace MultiFactor.SelfService.Linux.Portal.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class IsAuthorizedAttribute
    {
        private bool _autorizeСore;
        private readonly bool _validateUserSession;

        /// <summary>
        /// Creates instance of attribute.
        /// </summary>
        /// <param name="validateUserSession">true: validate cookies, JWT claims and that user is authenticated. false: validate that user is authenticated only.</param>
        public IsAuthorizedAttribute(bool validateUserSession = true)
        { }
    }
}
