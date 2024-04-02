using OperationGuidance_new.Constants;

namespace OperationGuidance_new.Tasks {
    public abstract class ATaskBase {
        #region Readonly fields
        protected string? _device_name = "";
        protected readonly int LoopingInterval = 25;
        public readonly int AuotReconnectingTrialDelay = 1000; // 断线重连尝试间隔
        public static readonly int DISCONNECTED = 0;
        public static readonly int CONNECTING = 1;
        public static readonly int CONNECTED = 2;
        #endregion
        
        #region Properties
        public string Name => _device_name ?? "";
        public DeviceTypeBase DeviceType { get; set; } = new(-1, "未设置");
        public abstract bool Connected { get; }
        public int Status { get; set; }
        public bool CloseConnectionManually { get; set; } = false;
        #endregion

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

        #region Common reusable methods
        public byte[] HexStrToBytes(string hexStr) => Enumerable.Range(0, hexStr.Length / 2)
            .Select(x => Convert.ToByte(hexStr.Substring(x * 2, 2), 16))
            .ToArray();
        public string BytesToHexStr(byte[] bytes) => Convert.ToHexString(bytes).ToLower();
        #endregion
    }
}
