namespace OperationGuidance_new.Tasks {
    public abstract class ATaskBase {
        #region Readonly fields
        private int _deviceId;
        protected string? _device_name = "";
        protected readonly int LoopingInterval = 25;
        public readonly int AuotReconnectingTrialDelay = 1000; // 断线重连尝试间隔
        public static readonly int DISCONNECTED = 0;
        public static readonly int CONNECTING = 1;
        public static readonly int CONNECTED = 2;
        #endregion

        #region Properties
        public string Name => _device_name ?? "";
        public abstract bool Connected { get; }
        public int Status { get; set; }
        public bool CloseConnectionManually { get; set; } = false;
        public int DeviceId { get => _deviceId; set => _deviceId = value; }
        #endregion

        public ATaskBase(int deviceId) {
            _deviceId = deviceId;
        }

        #region Main methods
        protected abstract void RunTask();
        public abstract Task Connect();
        // Can await util socket is connected
        public Task ConnectAsync() {
            return Connect();
        }
        public abstract void CloseConnection();
        public abstract bool WorkplaceCheckConnection();
        #endregion
    }
}
