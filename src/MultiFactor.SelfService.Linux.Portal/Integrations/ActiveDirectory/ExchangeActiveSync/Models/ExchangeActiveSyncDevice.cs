namespace MultiFactor.SelfService.Linux.Portal.Integrations.ActiveDirectory.ExchangeActiveSync.Models
{
    public class ExchangeActiveSyncDevice
    {
        public string MsExchDeviceId { get; private set; }
        public string FriendlyName { get; private set; }
        public string Model { get; private set; }
        public string Type { get; private set; }
        public DateTime WhenCreated { get; private set; }
        public ExchangeActiveSyncDeviceAccessState AccessState { get; private set; }
        public string AccessStateName { get; private set; }
        public string AccessStateReason { get; private set; }

        /// <summary>
        /// FriendlyName / Model / Type
        /// </summary>
        public string DisplayName
        {
            get
            {
                return new[]
                {
                    FriendlyName, Model, Type
                }
                .Where(el => !string.IsNullOrEmpty(el))
                .Distinct()
                .Aggregate((a, b) => a + " / " + b);
            }
        }

        public static ExchangeActiveSyncDeviceBuilder CreateBuilder()
        {
            return new ExchangeActiveSyncDeviceBuilder(new ExchangeActiveSyncDevice());
        }

        public class ExchangeActiveSyncDeviceBuilder
        {
            private readonly ExchangeActiveSyncDevice _device;

            public ExchangeActiveSyncDeviceBuilder(ExchangeActiveSyncDevice device)
            {
                _device = device ?? throw new ArgumentNullException(nameof(device));
            }

            public ExchangeActiveSyncDeviceBuilder SetMsExchDeviceId(string msExchDeviceId)
            {
                _device.MsExchDeviceId = msExchDeviceId;
                return this;
            }

            public ExchangeActiveSyncDeviceBuilder SetFriendlyName(string friendlyName)
            {
                _device.FriendlyName = friendlyName;
                return this;
            }

            public ExchangeActiveSyncDeviceBuilder SetModel(string model)
            {
                _device.Model = model;
                return this;
            }

            public ExchangeActiveSyncDeviceBuilder SetType(string type)
            {
                _device.Type = type;
                return this;
            }

            public ExchangeActiveSyncDeviceBuilder SetWhenCreated(DateTime whenCreated)
            {
                _device.WhenCreated = whenCreated;
                return this;
            }

            public ExchangeActiveSyncDeviceBuilder SetAccessState(ExchangeActiveSyncDeviceAccessState accessState)
            {
                _device.AccessState = accessState;
                return this;
            }

            public ExchangeActiveSyncDeviceBuilder SetAccessStateName(string accessStateName)
            {
                _device.AccessStateName = accessStateName;
                return this;
            }

            public ExchangeActiveSyncDeviceBuilder SetAccessStateReason(string accessStateReason)
            {
                _device.AccessStateReason = accessStateReason;
                return this;
            }

            public ExchangeActiveSyncDevice Build()
            {
                return _device;
            }
        }
    }
}
