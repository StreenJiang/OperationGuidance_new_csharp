using System.IO.Ports;
using System.Text;
using OperationGuidance_new.Constants;
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
        private DeviceTypeSerialPort _serialPort;
        private Action<string>? _actionAfterDataReceived;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => serialPortClient != null && serialPortClient.IsOpen && !CloseConnectionManually;
        // Other properties
        public string FullName { get => _device_name ?? ""; set => _device_name = value; }
        public string PortName { get => _portName; set => _portName = value; }
        public int BaudRate { get => _baudRate; set => _baudRate = value; }
        public Parity Parity { get => _parity; set => _parity = value; }
        public int DataBits { get => _dataBits; set => _dataBits = value; }
        public StopBits StopBits { get => _stopBits; set => _stopBits = value; }
        public string? Result { get; set; }
        public SerialPort? SerialPortClient { get => serialPortClient; }
        public Action<string>? ActionAfterDataReceived { get => _actionAfterDataReceived; set => _actionAfterDataReceived = value; }
        #endregion

        #region Constructors
        public SerialPortTask(string fullName, string portName, int baudRate, Parity parity, int dataBits, 
                StopBits stopBits, DataTypes dataType, DeviceTypeSerialPort serialPort) {
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
                    System.Console.WriteLine($"Error: {e}");
                } finally {
                    System.Console.WriteLine($"Disconnected to SerialPort[{_device_name}]");
                    if (serialPortClient != null) {
                        serialPortClient.Close();
                        serialPortClient = null;
                    }
                    if (CloseConnectionManually) {
                        System.Console.WriteLine($"Serial port device connection<SerialPort[{_device_name}] has been closed manually, won't try to reconnecte anymore.");
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
            System.Console.WriteLine($"Close SerialPort[{_device_name}] manually...");
            CloseConnectionManually = true;
            if (Connected) {
                serialPortClient.Close();
                Result = null;
            }
        }
        #endregion

        #region Methods
        private bool ConnectToSerialPortDevice() {
            try {
                System.Console.WriteLine($"Connecting to SerialPort[{_device_name}]");
                Dictionary<string, string> serialPorts = ConnectionUtils.GetSerialPorts();
                if (serialPorts.ContainsKey(_portName)) {
                    serialPortClient = new() {
                        PortName = _portName,
                        BaudRate = _baudRate,
                        Parity = _parity,
                        DataBits = _dataBits,
                        StopBits = _stopBits,
                        Encoding = Encoding.ASCII,
                    };
                    serialPortClient.DataReceived += (sender, eventArgs) => {
                        Thread.Sleep(500);
                        try {
                            byte[] data = new byte[2048];
                            int msgLen = serialPortClient.Read(data, 0, data.Length);
                            string result = Encoding.ASCII.GetString(data, 0, msgLen).Trim();
                            System.Console.WriteLine($"Data received from SerialPort[{_device_name}], result: {result}");
                            if (_actionAfterDataReceived != null) {
                                _actionAfterDataReceived(result);
                            }
                        } catch (Exception e) {
                            System.Console.WriteLine($"Error occurred whlie receiving data from SerialPort[{_device_name}], e: {e}");
                        }
                    };
                    serialPortClient.Open();
                    System.Console.WriteLine($"Successfully connect to SerialPort[{_device_name}]");
                    return true;
                } else {
                    System.Console.WriteLine($"Failed to connect to SerialPort[{_device_name}], can't find current serial port device.");
                    return false;
                }
            } catch (Exception e) {
                System.Console.WriteLine($"Failed to connect to SerialPort[{_device_name}], e: {e}");
                return false;
            }
        }
        #endregion
    }
}
