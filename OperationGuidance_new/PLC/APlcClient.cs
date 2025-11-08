using log4net;

namespace OperationGuidance_new.PLC {
    public abstract class APlcClient: IDisposable {
        private ILog log = LogManager.GetLogger(typeof(APlcClient));
        protected ModbusTcpClient _modbusTcpClient;

        public ModbusTcpClient ModbusTcpClient => _modbusTcpClient;

        public APlcClient(string ip, int port) {
            try {
                _modbusTcpClient = new();
                _modbusTcpClient.Connect(ip, port);
            } catch (Exception ex) {
                log.Error($"无法使用【ip={ip}，port={port}】连接到PLC！", ex);
                throw ex;
            }
        }

        public abstract void WriteResult(bool result);

        public void Dispose() {
            _modbusTcpClient?.Disconnect();
        }
    }
}
