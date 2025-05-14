using Microsoft.AspNetCore.Localization;
using MultiFactor.SelfService.Linux.Portal.Settings;
using MultiFactor.SelfService.Linux.Portal.Settings.PasswordRequirement;
using System.Text;

namespace MultiFactor.SelfService.Linux.Portal.Core
{
    public enum ContentCategory
    {
        None,
        PasswordRequirements
    }

    public class ContentCache
    {
        private static readonly string _directory = $"{Constants.WORKING_DIRECTORY}/Content";
        private readonly Lazy<Dictionary<string, string[]>> _loadedContent;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly PortalSettings _portalSettings;
        private readonly PasswordRequirementsService _passwordRequirementsService;

        public ContentCache(
            IHttpContextAccessor contextAccessor, 
            ILogger<ContentCache> logger,
            PortalSettings portalSettings,
            PasswordRequirementsService passwordRequirementsService)
        {
            _loadedContent = new Lazy<Dictionary<string, string[]>>(() =>
            {
                try
                {
                    if (!Directory.Exists(_directory))
                    {
                        Directory.CreateDirectory(_directory);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create content directory '{Directory:l}'", _directory);
                }

                var files = Directory.GetFiles(_directory, "*.content", SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                {
                    return new Dictionary<string, string[]>();
                }

                var dict = new Dictionary<string, string[]>();
                foreach (var file in files)
                {
                    try
                    {
                        var lines = File.ReadAllLines(file, Encoding.UTF8);
                        var key = Path.GetFileNameWithoutExtension(file);
                        dict[key] = lines;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to load content from file '{File:l}'", file);
                        continue;
                    }
                }

                return dict;
            });
            _contextAccessor = contextAccessor;
            _portalSettings = portalSettings;
            _passwordRequirementsService = passwordRequirementsService;
        }

        public string[] GetLines(ContentCategory category)
        {
            if (category != ContentCategory.PasswordRequirements)
            {
                return Array.Empty<string>();
            }

            var requirements = new List<string>();

            var feature = _contextAccessor.HttpContext?.Features.Get<IRequestCultureFeature>();
            var culture = feature?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "en";
            
            if (_portalSettings.PasswordRequirements?.PwdRequirement != null)
            {
                foreach (var requirement in _portalSettings.PasswordRequirements.PwdRequirement.Where(r => r.Enabled))
                {
                    string description = null;

                    if (!string.IsNullOrEmpty(requirement.Condition))
                    {
                        var rule = _passwordRequirementsService.GetRule(requirement.Condition);
                        if (rule != null)
                        {
                            description = rule.GetLocalizedDescription();
                        }
                    }

                    if (string.IsNullOrEmpty(description))
                    {
                        description = culture == "ru" ? requirement.DescriptionRu : requirement.DescriptionEn;
                    }

                    if (!string.IsNullOrEmpty(description))
                    {
                        requirements.Add(description);
                    }
                }
            }

            // Если не нашли реков в конфиге, возвращаемся к чтению из файлов
            if (requirements.Count == 0)
            {
                var key = GetKey(category, culture);
                return _loadedContent.Value.ContainsKey(key) ? _loadedContent.Value[key] : Array.Empty<string>();
            }

            return requirements.ToArray();
        }

        private static string GetKey(ContentCategory category, string culture)
        {
            switch (category)
            {
                case ContentCategory.PasswordRequirements:
                    return $"pwd.{culture}";

                case ContentCategory.None:
                default:
                    return string.Empty;
            }
        }
    }
}
