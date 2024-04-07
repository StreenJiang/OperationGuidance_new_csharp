using System.Globalization;
using System.Net;
using System.Net.Sockets;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class ArmTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(ArmTask));

        #region Fields
        private readonly int ReceiveTimeout = 2000;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeArm _armType;
        private Coordinates3D? _currentCoordinates;
        private Action<Coordinates3D> _actionAfterReceiving;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually;
        // Other properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public DeviceTypeArm ArmType { get => _armType; set => _armType = value; }
        public Queue<string> Commands { get; set; } = new();
        public string? Result { get; set; }
        public bool RetrieveResult { get; set; } = false;
        public Action<Coordinates3D> ActionAfterReceiving { get => _actionAfterReceiving; set => _actionAfterReceiving = value; }

        public event Action<Coordinates3D> OnActionAfterReceiving { add => _actionAfterReceiving += value; remove => _actionAfterReceiving -= value; }
        #endregion

        #region Constructors
        public ArmTask(string? name, string ip, int port, DeviceTypeArm arm) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _armType = arm;
            DeviceType = arm;
            _actionAfterReceiving += c => {};
            Status = DISCONNECTED;
        }
        #endregion

        #region Override methods
        protected override void RunTask() {
            Task.Run(async () => {
                try {
                    while (Connected) {
                        // Check if any command
                        if (Commands.Count > 0) {
                            string command = Commands.Dequeue();
                            // Send command to controller
                            await socketClient.SendAsync(HexStrToBytes(command), SocketFlags.None);
                            // Check response
                            try {
                                byte[] msgBytes = new byte[1024 * 1024];
                                int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                                if (msgLen >= 5) {
                                    Result = BytesToHexStr(msgBytes.Take(msgLen).ToArray());
                                } else {
                                    Result = "";
                                }
                            } catch (Exception e) {
                                System.Console.WriteLine($"No data received...");
                            }
                        }
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while running task for connection<ARM[{_device_name} - {_ip}: {_port}]>, e: {e}");
                } finally {
                    logger.Info($"Disconnected to ARM[{_device_name} - {_ip}: {_port}]");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                        Commands.Clear();
                    }
                    if (CloseConnectionManually) {
                        logger.Info($"Socket connection<ARM[{_device_name} - {_ip}: {_port}]> has been closed manually, won't try to reconnecte anymore.");
                    }
                }
            });
        }
        public override Task Connect() {
            return Task.Run(async () => {
                while (!Connected && !CloseConnectionManually) {
                    Status = CONNECTING;
                    if (ConnectToServer()) {
                        RunTask();
                        RunLoop();
                        Status = CONNECTED;
                        break;
                    }
                    await Task.Delay(AuotReconnectingTrialDelay);
                }
            });
        }
        public override void CloseConnection() {
            logger.Info($"Close connection<ARM[{_device_name} - {_ip}: {_port}]> manually...");
            if (Connected) {
                socketClient.Close();
            }
            CloseConnectionManually = true;
            Result = null;
            Commands.Clear();
        }
        public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        #endregion

        #region Methods
        private bool ConnectToServer() {
            if (Connected) {
                logger.Warn($"Already connecting to ARM[{_device_name} - {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            logger.Info($"Connecting to ARM[{_device_name} - {_ip}: {_port}]");
            bool pingSuccess = false;
            bool connectSuccess = false;

            // 1. check ping
            pingSuccess = MainUtils.PingHost(_ip);
            if (pingSuccess) {
                // 2. check socket
                try {
                    socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = ReceiveTimeout;
                    socketClient.Connect(IPAddress.Parse(_ip), _port);
                    connectSuccess = true;
                    MainUtils.Info(logger, $"Successfully connect to ARM[{_device_name} - {_ip}: {_port}]");
                } catch (Exception e) {
                    logger.Warn($"Error while connecting to ARM[{_device_name} - {_ip}: {_port}]: {e}");
                }
            } else {
                logger.Warn($"Failed to ping ARM[{_device_name} - {_ip}: {_port}]");
            }
            return pingSuccess && connectSuccess;
        }
        public void SendCommand(string command) {
            Commands.Enqueue(command);
        }
        public async Task<string?> SendCommandAsync(string command) {
            Result = null;
            SendCommand(command);

            string? result = null;
            int trialTime = 0;
            while (Connected && result == null) {
                if (trialTime > ReceiveTimeout) {
                    logger.Error("Can't get any response at all, probably some other connection robbed it...");
                    break;
                }
                result = Result;
                if (result == null) {
                    await Task.Delay(LoopingInterval);
                    trialTime += LoopingInterval;
                }
            }
            Result = null;
            return result;
        }
        public async Task<Coordinates3D?> GetCurrentCoordinates() { 
            RetrieveResult = true;
            await Task.Delay(100);
            Coordinates3D? coordinates = _currentCoordinates;
            RetrieveResult = false;
            return coordinates;
        }
        private async Task<Coordinates3D?> GetCoordinatesAsync() {
            Coordinates3D? coordinates = null;
            if (Connected) {
                coordinates = new();
                string? x = await SendCommandAsync(_armType.COMMAND_READ_X_HEX.GetMessage());
                if (x != null) {
                    coordinates.X = ParseResult(x);
                }
                string? y = await SendCommandAsync(_armType.COMMAND_READ_Y_HEX.GetMessage());
                if (y != null) {
                    coordinates.Y = ParseResult(y);
                }
                if (_armType.COMMAND_READ_Z_HEX != null) {
                    string? z = await SendCommandAsync(_armType.COMMAND_READ_Z_HEX.GetMessage());
                    if (z != null) {
                        coordinates.Z = ParseResult(z);
                    }
                }
                if (_actionAfterReceiving.GetInvocationList().Length > 0) {
                    _actionAfterReceiving(coordinates);
                }
            }
            return coordinates;
        }
        private void RunLoop() {
            Task.Run(async () => {
                try {
                    while (Connected) {
                        if (RetrieveResult) {
                            _currentCoordinates = await GetCoordinatesAsync();
                        } else {
                            await Task.Delay(LoopingInterval);
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Error occurred while looping for coordinates for ARM[{_device_name} - {_ip}: {_port}], e: {e}");
                    throw e;
                } finally {
                    logger.Error("Loop stops  for ARM[{_device_name} - {_ip}: {_port}]...");
                }
            });
        }
        private int ParseResult(string result) {
            int coordinate = 0;
            if (result != null && result != "") {
                string lowData = result.Substring(6, 4);
                string HighData = result.Substring(10, 4);
                if (lowData != "ffff" && HighData != "ffff") {
                    coordinate = int.Parse(lowData, NumberStyles.HexNumber);
                    // coordinate = Convert.ToInt32(lowData, 16);
                }
            }
            return coordinate;
        }
        #endregion
    }
}
