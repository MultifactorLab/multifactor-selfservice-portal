using Microsoft.Extensions.Options;
using MultiFactor.SelfService.Linux.Portal.Settings;

namespace MultiFactor.SelfService.Linux.Portal.Options
{
    public interface IShowcaseSettingsOptions
    {
        ShowcaseSettings? CurrentValue { get; }
    }

    public class ShowcaseSettingsOptions : IShowcaseSettingsOptions
    {
        public ShowcaseSettings? CurrentValue => _monitor.CurrentValue;
        private readonly IOptionsMonitor<ShowcaseSettings?> _monitor;

        public ShowcaseSettingsOptions(IOptionsMonitor<ShowcaseSettings?> monitor)
        {
            _monitor = monitor;
        }
    }
}
