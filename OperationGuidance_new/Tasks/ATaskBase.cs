namespace OperationGuidance_new.Tasks {
    public abstract class ATaskBase {
        #region Readonly fields
        protected readonly int LoopingInterval = 25;
        protected readonly int AuotReconnectingTrialDelay = 1000; // 断线重连尝试间隔
        #endregion
        
        #region Properties
        public abstract bool Connected { get; }
        public bool CloseConnectionManually { get; set; } = false;
        #endregion

        #region Main methods
        protected abstract void RunTask();
        public abstract void Connect();
        // Can await util socket is connected
        public async Task ConnectAsync() {
            Connect();
            while (true) {
                if (Connected) return;
                await Task.Delay(AuotReconnectingTrialDelay);
            }
        }
        public abstract void CloseConnection();
        #endregion

        #region Common reusable methods
        public byte[] HexStrToBytes(string hexStr) => Enumerable.Range(0, hexStr.Length / 2)
            .Select(x => Convert.ToByte(hexStr.Substring(x * 2, 2), 16))
            .ToArray();
        public string BytesToHexStr(byte[] bytes) => Convert.ToHexString(bytes).ToLower();
        #endregion
    }
}
