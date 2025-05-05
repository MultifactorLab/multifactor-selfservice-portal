namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ShowcaseLinks
    {
        [ConfigurationKeyName(nameof(Link))]
        private List<Link> Links { get; set; } = new List<Link>();

        [ConfigurationKeyName(nameof(Link))]
        private Link SingleLink { get; set; } = new Link();

        public bool Log { get; private set; }

        public List<Link> GetLinks()
        {
            // To deal with a single element binding to array issue, we should map a single item manually 
            // See: https://github.com/dotnet/runtime/issues/57325
            if (Links.Count == 0 && !string.IsNullOrEmpty(SingleLink.Title))
            {
                Links.Add(SingleLink);
            }
            return Links;
        }
    }

    public class Link
    {
        public string Url { get; internal set; }
        public string Title { get; internal set; }
        public string Image { get; internal set; }
        public bool OpenInNewTab { get; internal set; }
    }
}
