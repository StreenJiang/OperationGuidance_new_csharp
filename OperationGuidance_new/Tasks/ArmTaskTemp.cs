using System.Globalization;
using System.Net;
using System.Net.Sockets;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AsbtractClasses;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class ArmTaskTemp: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(ArmTaskTemp));

        #region Fields
        private static readonly object SendSyncRoot = new();
        private static readonly object ReceiveSyncRoot = new();
        private readonly int ReceiveTimeout = 2000;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeArm _armType;
        private Coordinates3D? _currentCoordinates;
        private Action<Coordinates3D>? _actionAfterReceiving;
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
        public Action<Coordinates3D>? ActionAfterReceiving { get => _actionAfterReceiving; set => _actionAfterReceiving = value; }
        #endregion

        #region Constructors
        public ArmTaskTemp(int deviceId, string? name, string ip, int port, DeviceTypeArm arm, int? workstationId = null) : base(deviceId, workstationId) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _armType = arm;
            Status = DISCONNECTED;
        }
        #endregion

        #region Override methods
        protected void RegisterTCPClient() => MainUtils.RegisterTCPClient(_ip, _port, CommonUtils.CannotBeNull(socketClient));
        protected void DeregisterTCPClient() => MainUtils.DeregisterTCPClient(_ip, _port);
        protected override void RunTask() {
            Task.Run(async () => {
                try {
                    while (Connected) {
                        if (RetrieveResult) {
                            lock (MainUtils.GetTCPClientKey(_ip, _port)) {
                                _currentCoordinates = GetCoordinates();
                                if (_actionAfterReceiving != null && _actionAfterReceiving.GetInvocationList().Length > 0) {
                                    _actionAfterReceiving(_currentCoordinates);
                                }
                            }
                        }

                        await Task.Delay(LoopingInterval);
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
        public override async void Connect() {
            while (!Connected && !CloseConnectionManually) {
                Status = CONNECTING;
                if (ConnectToServer()) {
                    RegisterTCPClient();
                    RunTask();
                    Status = CONNECTED;
                    break;
                }
                await Task.Delay(AutoReconnectingTrialDelay);
            }
        }
        public override Task ConnectAsync() => Task.Run(() => Connect());
        public override void CloseConnection() {
            logger.Info($"Close connection<ARM[{_device_name} - {_ip}: {_port}]> manually...");
            if (Connected) {
                socketClient.Close();
                DeregisterTCPClient();
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

            // 0. Check if socket already registerd
            Socket? socket = MainUtils.GetTCPClient(_ip, _port);
            if (socket != null) {
                socketClient = socket;
                MainUtils.Info(logger, $"Successfully connect to ARM[{_device_name} - {_ip}: {_port}] (already registerd)");
                return true;
            }

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
        public string SendCommand(string command) {
            if (Connected) {
                try {
                    lock (SendSyncRoot) {
                        // Send command to controller
                        socketClient.Send(MainUtils.ToBytes(command));

                        // Receive data
                        lock (ReceiveSyncRoot) {
                            byte[] msgBytes = new byte[1024 * 1024];
                            int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                            string result = MainUtils.ToHexString(msgBytes.Take(msgLen).ToArray());
                            return result;
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Error while sending command[{command}] to IOBOX[{_device_name} - {_ip}: {_port}], e: {e}");
                    // throw e;
                }
            }
            return "";
        }
        public async Task<string> SendCommandAsync(string command) {
            return await Task.Run(() => SendCommand(command));
        }

        public Coordinates3D? GetCurrentCoordinates() => GetCoordinates();

        private Coordinates3D? GetCoordinates() {
            Coordinates3D? coordinates = null;
            if (Connected) {
                coordinates = new();
                string? x = SendCommand(_armType.COMMAND_READ_X_HEX.GetMessage());
                if (x != null) {
                    coordinates.X = ParseResult(x);
                }
                string? y = SendCommand(_armType.COMMAND_READ_Y_HEX.GetMessage());
                if (y != null) {
                    coordinates.Y = ParseResult(y);
                }
                if (_armType.COMMAND_READ_Z_HEX != null) {
                    string? z = SendCommand(_armType.COMMAND_READ_Z_HEX.GetMessage());
                    if (z != null) {
                        coordinates.Z = ParseResult(z);
                    }
                }
            }
            return coordinates;
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
