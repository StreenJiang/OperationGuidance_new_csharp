using log4net;
using OperationGuidance_new.Utils;
using S7.Net;

namespace OperationGuidance_new.Constants {
    public abstract class PlcServerBase: IDisposable {
        private ILog logger;

        private Plc? _plc;
        private CpuType _cpuType;
        private string _ip;

        public Plc? Plc { get => _plc; set => _plc = value; }
        public CpuType CpuType { get => _cpuType; set => _cpuType = value; }
        public string Ip { get => _ip; set => _ip = value; }

        public PlcServerBase(CpuType cpuType, string ip) {
            logger = MainUtils.GetLogger(GetType());

            _cpuType = cpuType;
            _ip = ip;
        }

        public bool Connect() {
            try {
                _plc = new(_cpuType, _ip, 0, 1);
                _plc.Open();
                MainUtils.Info(logger, $"PLC with ip = {_ip} open successfully");
                return true;
            } catch (Exception e) {
                MainUtils.Warn(logger, $"Can't open PLC connection with ip = {_ip}");
                logger.Error($"Can't open PLC connection with ip = {_ip}, e = {e}");
            }
            return false;
        }

        public void Dispose() {
            if (_plc != null && _plc.IsConnected) {
                _plc.Close();
            }
        }
    }
}
