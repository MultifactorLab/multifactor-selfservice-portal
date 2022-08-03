namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync.Models
{
    public record ExchangeActiveSyncDeviceInfo(
        string DeviceId, 
        string DeviceDistinguishedName,
        string UserDistinguishedName
        )
    {
        public override string ToString()
        {
            return $"DeviceId:{DeviceId}, UserDN:{UserDistinguishedName}, DeviceDN:{DeviceDistinguishedName}";
        }
    }
}
