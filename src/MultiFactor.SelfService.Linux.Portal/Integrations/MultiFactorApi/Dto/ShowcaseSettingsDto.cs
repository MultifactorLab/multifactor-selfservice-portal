namespace MultiFactor.SelfService.Linux.Portal.Integrations.MultiFactorApi.Dto
{
    public class ShowcaseSettingsDto
    {
        public bool Enabled { get; init; }
        public IEnumerable<ShowcaseLinkDto> ShowcaseLinks { get; init; }
    }

    public class ShowcaseLinkDto
    {
        public string Url { get; init; }
        public string Title { get; init; }
        public string Image { get; init; }
        public bool OpenInNewTab { get; init; }
    }
}