using System.Text;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Abstracts;
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
        protected override async Task RunTaskAsync(CancellationToken cancellationToken = default) {
            try {
                while (!cancellationToken.IsCancellationRequested && Connected) {
                    // Just keep running to keep it alive
                    await Task.Delay(LoopingInterval, cancellationToken);
                }
            } catch (OperationCanceledException) {
                logger.Info(MainUtils.FormatDeviceLog("SERIALPORT", _portName, $"Task execution cancelled for {_device_name}"));
            } catch (Exception e) {
                logger.Warn(MainUtils.FormatDeviceLog("SERIALPORT", _portName, $"Error while running task for {_device_name}: {e.Message}"));
            } finally {
                logger.Info(MainUtils.FormatDeviceLog("SERIALPORT", _portName, $"Disconnected from {_device_name}"));
                if (serialPortStreamClient != null) {
                    serialPortStreamClient.Close();
                    serialPortStreamClient = null;
                }
                if (CloseConnectionManually) {
                    logger.Info(MainUtils.FormatDeviceLog("SERIALPORT", _portName, $"Connection closed manually for {_device_name}, won't reconnect"));
                }
            }
        }

        public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
            return await ConnectWithRetryAsync(async (ct) => {
                if (await ConnectToSerialPortDeviceAsync(ct)) {
                    // Start the task loop
                    _ = Task.Run(async () => {
                        await RunTaskAsync(cancellationToken);
                    }, cancellationToken);

                    Status = CONNECTED;
                    logger.Info(MainUtils.FormatDeviceLog("SERIALPORT", _portName, "Connection established successfully"));
                    return true;
                }
                return false;
            }, cancellationToken: cancellationToken);
        }

        public override async Task CloseConnectionAsync(CancellationToken cancellationToken = default) {
            await Task.Run(() => {
                logger.Info(MainUtils.FormatDeviceLog("SERIALPORT", _portName, $"Close connection manually for {_device_name}"));
                if (Connected) {
                    serialPortStreamClient.Close();
                }
                CloseConnectionManually = true;
                Result = null;
            }, cancellationToken);
        }

        public override bool WorkplaceCheckConnection() => Connected;
        public override async Task<bool> WorkplaceCheckConnectionAsync(CancellationToken cancellationToken = default) {
            return await Task.FromResult(Connected);
        }
        #endregion

        #region Methods
        private async Task<bool> ConnectToSerialPortDeviceAsync(CancellationToken cancellationToken = default) {
            try {
                logger.Info($"[SERIALPORT] Starting connection process - Port: {_portName}, BaudRate: {_baudRate}");
                cancellationToken.ThrowIfCancellationRequested();

                Dictionary<string, string> serialPorts = ConnectionUtils.GetSerialPorts();
                if (!serialPorts.ContainsKey(_portName)) {
                    logger.Warn($"[SERIALPORT] Failed to connect - port {_portName} not found");
                    return false;
                }

                logger.Debug($"[SERIALPORT] Port {_portName} found, opening connection...");
                serialPortStreamClient = new(_portName, _baudRate, _dataBits, _parity, _stopBits);
                serialPortStreamClient.DataReceived += async (sender, eventArgs) => {
                    try {
                        await Task.Delay(200, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();

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
                    } catch (OperationCanceledException) {
                        // Silently ignore cancellation during data receive
                    } catch (Exception e) {
                        logger.Error($"Error occurred while receiving data from SerialPort[{_device_name}], e: {e}");
                    }
                };

                // Check for cancellation before opening
                cancellationToken.ThrowIfCancellationRequested();

                // Open the serial port (synchronous but quick)
                serialPortStreamClient.Open();

                logger.Info($"[SERIALPORT] Successfully connected to {_device_name} (Port: {_portName}, BaudRate: {_baudRate})");
                return true;
            } catch (OperationCanceledException) {
                logger.Info($"[SERIALPORT] Connection cancelled for {_portName}");
                if (serialPortStreamClient != null && serialPortStreamClient.IsOpen) {
                    serialPortStreamClient.Close();
                    serialPortStreamClient = null;
                }
                throw; // Re-throw to let ConnectWithRetryAsync handle it
            } catch (Exception e) {
                logger.Warn($"[SERIALPORT] Failed to connect to {_device_name}: {e.Message}");
                if (serialPortStreamClient != null && serialPortStreamClient.IsOpen) {
                    serialPortStreamClient.Close();
                    serialPortStreamClient = null;
                }
                return false;
            }
        }

        // Backward compatibility wrapper (deprecated)
        private bool ConnectToSerialPortDevice(CancellationToken cancellationToken = default) {
            try {
                return ConnectToSerialPortDeviceAsync(cancellationToken).GetAwaiter().GetResult();
            } catch (AggregateException ex) {
                throw ex.InnerException ?? ex;
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
