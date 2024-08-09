using log4net;
using OperationGuidance_new.Utils;
using S7.Net;

namespace OperationGuidance_new.Constants {
    public abstract class PlcServerBase: IDisposable {
        private ILog logger;

        private Plc? _plc;
        private CpuType _cpuType;
        private string _ip;
        private int _db;
        private string _address;
        private int _bitAddress;
        private int _dataLength;
        private byte[]? _dataBytes;

        public Plc? Plc { get => _plc; set => _plc = value; }
        public CpuType CpuType { get => _cpuType; set => _cpuType=value; }
        public string Ip { get => _ip; set => _ip=value; }
        public int Db { get => _db; set => _db = value; }
        public string Address { get => _address; set => _address = value; }
        public int BitAddress { get => _bitAddress; set => _bitAddress = value; }
        public int DataLength { get => _dataLength; set => _dataLength = value; }
        public byte[]? DataBytes { get => _dataBytes; set => _dataBytes = value; }

        public PlcServerBase(CpuType cpuType, string ip, int db, string address, int bitAddress, int dataLength) {
            logger = MainUtils.GetLogger(GetType());

            _cpuType = cpuType;
            _ip = ip;
            _db = db;
            _address = address;
            _bitAddress = bitAddress;
            _dataLength = dataLength;
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

        public void ReadBytes() {
            if (_plc != null) {
                _dataBytes = _plc.ReadBytes(DataType.DataBlock, _db, int.Parse(new String(_address.Skip(3).ToArray())), _dataLength);
            }
        }

        public void Dispose() {
            if (_plc != null) {
                _plc.Close();
            }
        }
    }
}
