using System.IO.Ports;
using System.Text;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class SerialPortTask: ATaskBase {
        #region Fields
        private new readonly int LoopingInterval = 5000;
        private string _portName;
        private int _baudRate;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;
        private SerialPort? serialPortClient;
        private DeviceSerialPort _serialPort;
        private Action<string>? _actionAfterDataReceived;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => serialPortClient != null && serialPortClient.IsOpen && !CloseConnectionManually;
        // Other properties
        public string FullName { get => _device_name ?? ""; set => _device_name = value; }
        public string PortName { get => _portName; set => _portName = value; }
        public string? Result { get; set; }
        public SerialPort? SerialPortClient { get => serialPortClient; }
        public Action<string>? ActionAfterDataReceived { get => _actionAfterDataReceived; set => _actionAfterDataReceived = value; }
        #endregion

        #region Constructors
        public SerialPortTask(string fullName, string portName, int baudRate, Parity parity, int dataBits, 
                StopBits stopBits, DataTypes dataType, DeviceSerialPort serialPort) {
            _device_name = fullName;
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _serialPort = serialPort;
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
                    MainUtils.PrintEventLog($"Error: {e}");
                } finally {
                    MainUtils.PrintEventLog($"Disconnected to SerialPort[{_device_name}]");
                    if (serialPortClient != null) {
                        serialPortClient.Close();
                        serialPortClient = null;
                    }
                    if (CloseConnectionManually) {
                        MainUtils.PrintEventLog($"Serial port device connection<SerialPort[{_device_name}]> has been closed manually, won't try to reconnecte anymore.");
                    }
                }
            });
        }
        public override Task Connect() {
            return Task.Run(async () => {
                while (!Connected) {
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
            MainUtils.PrintEventLog($"Close serial port<SerialPort[{_device_name}]> manually...");
            if (Connected) {
                CloseConnectionManually = true;
                serialPortClient.Close();
                Result = null;
            }
        }
        #endregion

        #region Methods
        private bool ConnectToSerialPortDevice() {
            try {
                Dictionary<string, string> serialPorts = ConnectionUtils.GetSerialPorts();
                if (serialPorts.ContainsKey(_portName)) {
                    serialPortClient = new() {
                        PortName = _portName,
                        BaudRate = _baudRate,
                        Parity = _parity,
                        DataBits = _dataBits,
                        StopBits = _stopBits,
                    };
                    serialPortClient.DataReceived += (sender, eventArgs) => {
                        Thread.Sleep(500);
                        try {
                            byte[] data = new byte[2048];
                            int msgLen = serialPortClient.Read(data, 0, data.Length);
                            string result = Encoding.ASCII.GetString(data, 0, msgLen).Trim();
                            MainUtils.PrintEventLog($"Data received from serial port<SerialPort[{_device_name}]>, result: {result}");
                            if (_actionAfterDataReceived != null) {
                                _actionAfterDataReceived(result);
                            }
                        } catch (Exception e) {
                            MainUtils.PrintEventLog($"Error occurred whlie receiving data from serial port<SerialPort[{_device_name}]>, e: {e}");
                        }
                    };
                    serialPortClient.Open();
                    return true;
                } else {
                    return false;
                }
            } catch (Exception e) {
                MainUtils.PrintEventLog($"Failed to connect to SerialPort[{_device_name}], e: {e}");
                return false;
            }
        }
        #endregion
    }
}
