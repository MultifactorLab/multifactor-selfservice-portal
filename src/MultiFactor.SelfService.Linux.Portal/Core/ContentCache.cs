using Microsoft.AspNetCore.Localization;
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

        public ContentCache(IHttpContextAccessor contextAccessor, ILogger<ContentCache> logger)
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
        }

        public string[] GetLines(ContentCategory category)
        {
            var feature = _contextAccessor.HttpContext?.Features.Get<IRequestCultureFeature>();
            var culture = feature?.RequestCulture.Culture.TwoLetterISOLanguageName ?? "en";

            var key = GetKey(category, culture);
            return _loadedContent.Value.ContainsKey(key) ? _loadedContent.Value[key] : Array.Empty<string>();
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
