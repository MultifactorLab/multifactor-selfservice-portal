using MultiFactor.SelfService.Linux.Portal.Settings.PrivacyMode;

namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ShowcaseSettings
    {
        public bool Enabled { get; init; }
        public ShowcaseLink[] Links { get; init; } = [];
    }

    public class ShowcaseLink
    {
        public string Url { get; init; }
        public string Title { get; init; }
        public string Image { get; init; }
        public bool OpenInNewTab { get; init; }
    }
}
