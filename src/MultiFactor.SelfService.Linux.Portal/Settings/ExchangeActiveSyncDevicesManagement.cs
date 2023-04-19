namespace MultiFactor.SelfService.Linux.Portal.Settings
{
    public class ExchangeActiveSyncDevicesManagement
    { 
        public bool Enabled { get; private set; } = false;

        public ExchangeActiveSyncDevicesManagement() { }
        public ExchangeActiveSyncDevicesManagement(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
