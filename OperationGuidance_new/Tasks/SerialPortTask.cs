using System.IO.Ports;
using System.Text;
using OperationGuidance_new.Constants;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class SerialPortTask: ATaskBase {
        #region Fields
        private new readonly int LoopingInterval = 5000;
        private string _fullName;
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
        public string FullName { get => _fullName; set => _fullName = value; }
        public string PortName { get => _portName; set => _portName = value; }
        public string? Result { get; set; }
        public SerialPort? SerialPortClient { get => serialPortClient; }
        public Action<string>? ActionAfterDataReceived { get => _actionAfterDataReceived; set => _actionAfterDataReceived = value; }
        #endregion

        #region Constructors
        public SerialPortTask(string fullName, string portName, int baudRate, Parity parity, int dataBits, 
                StopBits stopBits, DataTypes dataType, DeviceSerialPort serialPort) {
            _fullName = fullName;
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _serialPort = serialPort;
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
                    System.Console.WriteLine($"Disconnected to {_fullName}");
                    if (serialPortClient != null) {
                        serialPortClient.Close();
                        serialPortClient = null;
                    }
                    if (CloseConnectionManually) {
                        System.Console.WriteLine($"Serial port device connection<{_fullName}> has been closed manually, won't try to reconnecte anymore.");
                    }
                }
            });
        }
        public override void Connect() {
            Task.Run(async () => {
                while (true) {
                    if (!Connected) {
                        if (ConnectToSerialPortDevice()) {
                            RunTask();
                        } else {
                            System.Console.WriteLine($"Trying to reconnect to serial port<{_fullName}>...");
                        }
                    }
                    await Task.Delay(AuotReconnectingTrialDelay);
                }
            });
        }
        public override void CloseConnection() {
            if (Connected) {
                CloseConnectionManually = true;
                serialPortClient.Close();
                Result = null;
                System.Console.WriteLine($"Close serial port<{_fullName}> manually...");
            } else {
                System.Console.WriteLine($"Serial port<{_fullName}> already closed...");
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
                            System.Console.WriteLine($"Data received from serial port<{_fullName}>, result: {result}");
                            if (_actionAfterDataReceived != null) {
                                _actionAfterDataReceived(result);
                            }
                        } catch (Exception e) {
                            System.Console.WriteLine($"Error occurred whlie receiving data from serial port<{_fullName}>, e: {e}");
                        }
                    };
                    serialPortClient.Open();
                    return true;
                } else {
                    return false;
                }
            } catch (Exception e) {
                System.Console.WriteLine($"Failed to connect to {_fullName}, e: {e}");
                return false;
            }
        }
        #endregion
    }
}
