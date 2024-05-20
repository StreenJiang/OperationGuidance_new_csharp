using System.Net;
using System.Net.Sockets;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class IoBoxTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(IoBoxTask));

        #region Fields
        private readonly int ReceiveTimeout = 500;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeIoBox _ioBoxType;
        private Action<string, int>? _ioBoxActionAfterAnalysis;
        private string _realTimeResult;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually;
        // Other properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public DeviceTypeIoBox IoBoxType { get => _ioBoxType; set => _ioBoxType = value; }
        public Queue<string> Command { get; } = new();
        public Queue<string> Result { get; } = new();
        public string RealTimeResult { get => _realTimeResult; set => _realTimeResult = value; }
        public bool RetrieveResult { get; set; } = false;
        public bool Locked { get; set; }
        public Action<string, int>? IoBoxActionAfterAnalysis { get => _ioBoxActionAfterAnalysis; set => _ioBoxActionAfterAnalysis = value; }
        #endregion

        #region Constructors
        public IoBoxTask(int deviceId, string? name, string ip, int port, DeviceTypeIoBox deviceType) : base(deviceId, null) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _ioBoxType = deviceType;
            Locked = false;
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
                        // Check if any command
                        if (Command.Count <= 0) {
                            // Read
                            _realTimeResult = SendCommand(_ioBoxType.COMMAND_READ.GetMessage());
                            if (RetrieveResult && _ioBoxActionAfterAnalysis != null) {
                                _ioBoxType.AnalyzeData(_realTimeResult, _ioBoxActionAfterAnalysis, DeviceId);
                            }
                        } else {
                            // Write
                            Result.Enqueue(SendCommand(Command.Dequeue()));
                        }

                        await Task.Delay(LoopingInterval);
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while running task for connection<IOBOX[{_device_name} - {_ip}: {_port}]>: {e}");
                } finally {
                    logger.Info($"Disconnected to IOBOX[{_device_name} - {_ip}: {_port}]");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                    if (CloseConnectionManually) {
                        logger.Info($"Socket connection<IOBOX[{_device_name} - {_ip}: {_port}]> has been closed manually, won't try to reconnecte anymore.");
                    }
                }
            });
        }
        public override Task Connect() {
            return Task.Run(async () => {
                while (!Connected && !CloseConnectionManually) {
                    Status = CONNECTING;
                    if (ConnectToServer()) {
                        RegisterTCPClient();
                        RunTask();
                        Status = CONNECTED;
                        break;
                    }
                    await Task.Delay(AuotReconnectingTrialDelay);
                }
            });
        }
        public override void CloseConnection() {
            logger.Info($"Close connection<IOBOX[{_device_name} - {_ip}: {_port}]> manually...");
            if (Connected) {
                socketClient.Close();
                DeregisterTCPClient();
            }
            CloseConnectionManually = true;
            Command.Clear();
            Result.Clear();
        }
        public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        #endregion

        #region Methods
        private bool ConnectToServer() {
            if (Connected) {
                logger.Warn($"Already connecting to IOBOX[{_device_name} - {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            logger.Info($"Connecting to IOBOX[{_device_name} - {_ip}: {_port}]");
            bool pingSuccess = false;
            bool connectSuccess = false;

            // 0. Check if socket already registerd
            Socket? socket = MainUtils.GetTCPClient(_ip, _port);
            if (socket != null) {
                socketClient = socket;
                logger.Info($"Successfully connect to IOBOX[{_device_name} - {_ip}: {_port}]");
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
                    logger.Info($"Successfully connect to IOBOX[{_device_name} - {_ip}: {_port}]");
                } catch (Exception e) {
                    logger.Warn($"Error while connecting to IOBOX[{_device_name} - {_ip}: {_port}]: {e}");
                }
            } else {
                logger.Warn($"Failed to ping IOBOX[{_device_name} - {_ip}: {_port}]");
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
                            return MainUtils.ToHexString(msgBytes.Take(msgLen).ToArray());
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Error while sending command to IOBOX[{_device_name} - {_ip}: {_port}], e: {e}");
                    // throw e;
                }
            }
            return "";
        }
        public async Task<string> SendCommandAsync(string command) {
            return await Task.Run(() => SendCommand(command));
        }
        public async Task<string> WriteToServer(string req) {
            Command.Enqueue(req);
            int waitCount = 0;
            while (Result.Count <= 0) {
                waitCount += 100;
                await Task.Delay(100);

                if (waitCount > ReceiveTimeout) {
                    return "";
                }
            }
            return Result.Dequeue();
        }

        #endregion
    }
}
