using System.Net;
using System.Net.Sockets;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AsbtractClasses;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class CommunicationTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(CommunicationTask));

        #region Fields
        private static readonly object SendSyncRoot = new();
        private static readonly object ReceiveSyncRoot = new();
        private readonly int ReceiveTimeout = 500;
        private readonly int KeepAliveDelay = 200;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeCommunication _communicationType;
        private Action? _actionAfterAnalysis;
        private ReadRequestMessage ReadRequest = new();
        private ReadResponseMessage ReadResponse = new();
        private ModBusServerBase? _modBusServer;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually;
        // Other properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public DeviceTypeCommunication CommunicationType { get => _communicationType; set => _communicationType = value; }
        public Queue<ACommunicationMessage> Command { get; } = new();
        public Queue<byte[]> Result { get; } = new();
        public bool Locked { get; set; }
        public Action? ActionAfterAnalysis { get => _actionAfterAnalysis; set => _actionAfterAnalysis = value; }
        public ModBusServerBase? ModBusServer { get => _modBusServer; set => _modBusServer = value; }
        #endregion

        #region Constructors
        public CommunicationTask(int deviceId, string? name, string ip, int port, DeviceTypeCommunication deviceType, int? workstationId = null) : base(deviceId, workstationId) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _communicationType = deviceType;
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
                            ReadResponse.SourceData = SendCommand(ReadRequest);

                            byte[] bytes = ReadResponse.MessageData.Skip(ReadResponse.LengthOfSymbols).ToArray();
                            if (_modBusServer != null) {
                                _modBusServer.LoadData(bytes);
                            }
                        } else {
                            ACommunicationMessage req = Command.Dequeue();
                            Result.Enqueue(SendCommand(req));
                        }

                        await Task.Delay(KeepAliveDelay);
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while running task for connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]>: {e}");
                } finally {
                    logger.Info($"Disconnected to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                    if (CloseConnectionManually) {
                        logger.Info($"Socket connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]> has been closed manually, won't try to reconnecte anymore.");
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
            logger.Info($"Close connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]> manually...");
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
                logger.Warn($"Already connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            logger.Info($"Connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
            bool pingSuccess = false;
            bool connectSuccess = false;

            // 0. Check if socket already registerd
            Socket? socket = MainUtils.GetTCPClient(_ip, _port);
            if (socket != null) {
                socketClient = socket;
                logger.Info($"Successfully connect to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
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
                    logger.Info($"Successfully connect to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
                } catch (Exception e) {
                    logger.Warn($"Error while connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}]: {e}");
                }
            } else {
                logger.Warn($"Failed to ping COMMUNICATION[{_device_name} - {_ip}: {_port}]");
            }
            return pingSuccess && connectSuccess;
        }
        public byte[] SendCommand(ACommunicationMessage command) {
            if (Connected) {
                try {
                    lock (SendSyncRoot) {
                        // Send command to controller
                        socketClient.Send(command.MessageData);

                        // Receive data
                        lock (ReceiveSyncRoot) {
                            byte[] msgBytes = new byte[1024 * 1024];
                            int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                            return msgBytes.Take(msgLen).ToArray();
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Error while sending command[{command}] to COMMUNICATION[{_device_name} - {_ip}: {_port}], e: {e}");
                    // throw e;
                }
            }
            return new byte[0];
        }
        public async Task<byte[]> SendCommandAsync(ACommunicationMessage command) {
            return await Task.Run(() => SendCommand(command));
        }
        public async Task<byte[]> WriteToServer(ACommunicationMessage req) {
            Command.Enqueue(req);
            int waitCount = 0;
            while (Result.Count <= 0) {
                waitCount += 100;
                await Task.Delay(100);

                if (waitCount > ReceiveTimeout) {
                    return new byte[0];
                }
            }
            return Result.Dequeue();
        }

        #endregion
    }
}
