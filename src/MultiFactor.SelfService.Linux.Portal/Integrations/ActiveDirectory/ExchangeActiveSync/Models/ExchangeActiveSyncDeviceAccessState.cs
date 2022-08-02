namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync.Models
{
    //https://social.technet.microsoft.com/Forums/en-US/016ed5b5-d26d-48e7-8c1f-613de81e6d91/how-can-i-use-the-shell-to-get-a-list-of-all-quarantined-devices-in-2010-sp1?forum=exchangesvrmobilitylegacy
    public enum ExchangeActiveSyncDeviceAccessState
    {
        Allowed = 1,
        Blocked = 2,
        Quarantined = 3,
        TestActiveSyncConnectivity = 4
    }
}
