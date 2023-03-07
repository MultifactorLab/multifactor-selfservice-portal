using System.Globalization;
using Microsoft.AspNetCore.Localization;
using MultiFactor.SelfService.Linux.Portal.Settings;

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