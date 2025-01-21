using System.Globalization;

namespace MultiFactor.SelfService.Linux.Portal.Core.Configuration.Providers
{
    public class ApplicationLanguageProvider
    {
        public string GetLanguage()
        {
            return CultureInfo.CurrentCulture.Name;
        }
    }   
}