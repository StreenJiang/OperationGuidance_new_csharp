using System.Text;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Utils;
using RJCP.IO.Ports;

namespace OperationGuidance_new.Tasks {
    public class SerialPortTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(SerialPortTask));

        #region Fields
        private new readonly int LoopingInterval = 5000;
        private string _portName;
        private int _baudRate;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;
        private DataTypes _dataType;
        private SerialPortStream? serialPortStreamClient;
        private DeviceTypeSerialPort _serialPortType;
        private Action<string>? _actionAfterDataReceived;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => serialPortStreamClient != null && serialPortStreamClient.IsOpen && !CloseConnectionManually;
        // Other properties
        public string FullName { get => _device_name ?? ""; set => _device_name = value; }
        public string PortName { get => _portName; set => _portName = value; }
        public int BaudRate { get => _baudRate; set => _baudRate = value; }
        public Parity Parity { get => _parity; set => _parity = value; }
        public int DataBits { get => _dataBits; set => _dataBits = value; }
        public StopBits StopBits { get => _stopBits; set => _stopBits = value; }
        public DataTypes DataType { get => _dataType; set => _dataType = value; }
        public DeviceTypeSerialPort SerialPortType { get => _serialPortType; set => _serialPortType = value; }
        public string? Result { get; set; }
        public SerialPortStream? SerialPortStreamClient { get => serialPortStreamClient; }
        public Action<string>? ActionAfterDataReceived { get => _actionAfterDataReceived; set => _actionAfterDataReceived = value; }
        #endregion

        #region Constructors
        public SerialPortTask(int deviceId, string fullName, string portName, int baudRate, Parity parity, int dataBits,
                StopBits stopBits, DataTypes dataType, DeviceTypeSerialPort serialPort, int? workstationId = null) : base(deviceId, workstationId) {
            _device_name = fullName;
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _dataType = dataType;
            _serialPortType = serialPort;
            Status = DISCONNECTED;
        }
        #endregion

        #region Override methods
        protected override void RunTask() {
            Task.Run(async () => {
                try {
                    while (Connected) {
                        // Just keep running to keep it alive
                        await Task.Delay(LoopingInterval);
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while running task for connection<SerialPort[{_device_name}]>, e: {e}");
                } finally {
                    logger.Info($"Disconnected to SerialPort[{_device_name}]");
                    if (serialPortStreamClient != null) {
                        serialPortStreamClient.Close();
                        serialPortStreamClient = null;
                    }
                    if (CloseConnectionManually) {
                        logger.Info($"Serial port device connection<SerialPort[{_device_name}] has been closed manually, won't try to reconnecte anymore.");
                    }
                }
            });
        }
        public override Task Connect() {
            return Task.Run(async () => {
                while (!Connected && !CloseConnectionManually) {
                    Status = CONNECTING;
                    if (ConnectToSerialPortDevice()) {
                        RunTask();
                        Status = CONNECTED;
                        break;
                    }
                    await Task.Delay(AuotReconnectingTrialDelay);
                }
            });
        }
        public override void CloseConnection() {
            logger.Info($"Close SerialPort[{_device_name}] manually...");
            if (Connected) {
                serialPortStreamClient.Close();
            }
            CloseConnectionManually = true;
            Result = null;
        }
        public override bool WorkplaceCheckConnection() => Connected;
        #endregion

        #region Methods
        private bool ConnectToSerialPortDevice() {
            try {
                logger.Info($"Connecting to SerialPort[{_device_name}]");
                Dictionary<string, string> serialPorts = ConnectionUtils.GetSerialPorts();
                if (serialPorts.ContainsKey(_portName)) {
                    serialPortStreamClient = new(_portName, _baudRate, _dataBits, _parity, _stopBits);
                    serialPortStreamClient.DataReceived += async (sender, eventArgs) => {
                        try {
                            await Task.Delay(200);
                            byte[] data = new byte[serialPortStreamClient.BytesToRead];
                            int msgLen = serialPortStreamClient.Read(data, 0, data.Length);
                            bool foundInvalidChar = false;
                            string result;
                            switch (_dataType) {
                                case DataTypes.ASCII:
                                    for (int i = 0; i < data.Length; i++) {
                                        byte b = data[i];
                                        if ((i > 0 && i < data.Length - 1) && (b < 32 || b > 126)) {
                                            foundInvalidChar = true;
                                            break;
                                        }
                                    }
                                    result = Encoding.ASCII.GetString(data, 0, msgLen).Trim().Trim('\x02').Trim('\x03');
                                    break;
                                case DataTypes.BINARY:
                                    result = ConvertToString(data, 2, out foundInvalidChar);
                                    break;
                                case DataTypes.OCTAL:
                                    result = ConvertToString(data, 8, out foundInvalidChar);
                                    break;
                                case DataTypes.DECIMAL:
                                    result = ConvertToString(data, 10, out foundInvalidChar);
                                    break;
                                case DataTypes.HEX:
                                    result = ConvertToString(data, 16, out foundInvalidChar);
                                    break;
                                default:
                                    result = string.Empty;
                                    break;
                            }

                            if (_actionAfterDataReceived != null) {
                                if (foundInvalidChar) {
                                    logger.Warn($"Data received from SerialPort[{_device_name}] found invalid character(s), data: {result}");
                                }
                                _actionAfterDataReceived(result);
                            }
                        } catch (Exception e) {
                            logger.Error($"Error occurred whlie receiving data from SerialPort[{_device_name}], e: {e}");
                        }
                    };
                    serialPortStreamClient.Open();
                    MainUtils.Info(logger, $"Successfully connect to SerialPort[{_device_name}]");
                    return true;
                } else {
                    logger.Warn($"Failed to connect to SerialPort[{_device_name}], can't find current serial port device.");
                    return false;
                }
            } catch (Exception e) {
                logger.Warn($"Failed to connect to SerialPort[{_device_name}], e: {e}");
                return false;
            }
        }
        private string ConvertToString(byte[] data, int baseNum, out bool foundInvalidChar) {
            string result = "";
            foundInvalidChar = false;
            for (int i = 0; i < data.Length; i++) {
                byte b = data[i];
                if ((i > 0 && i < data.Length - 1) && (b < 32 || b > 126)) {
                    foundInvalidChar = true;
                    return "";
                }
                result += Convert.ToString(b, baseNum);
            }
            return result;
        }
        #endregion
    }
}
