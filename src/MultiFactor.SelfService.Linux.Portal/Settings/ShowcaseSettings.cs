using MultiFactor.SelfService.Linux.Portal.Settings.PrivacyMode;

namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ShowcaseSettings
    {
        public bool Enabled { get; set; }
        public ShowcaseLink[] Links { get; set; } = [];
    }

    public class ShowcaseLink
    {
        public string Url { get; internal set; }
        public string Title { get; internal set; }
        public string Image { get; internal set; }
        public bool OpenInNewTab { get; internal set; }
    }
}
