using System.Net;
using System.Net.Sockets;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.PLC;
using OperationGuidance_new.Tasks.AsbtractClasses;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Tasks {
    public class CommunicationTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(CommunicationTask));

        #region Fields
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
        private PlcServerBase? _plcServer;
        private APlcClient? _plcTcpClient;
        private bool _reading = false;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected {
            get {
                // if (_communicationType is CommunicationSiemensPlc) {
                //     return _plcServer != null && _plcServer.Plc.IsConnected;
                // }
                if (_communicationType is CommunicationModBusTcp) {
                    return _plcTcpClient is not null ? _plcTcpClient.IsConnected() : false;
                }
                return socketClient != null && socketClient.Connected && !CloseConnectionManually;
            }
        }
        // Other properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public DeviceTypeCommunication CommunicationType { get => _communicationType; set => _communicationType = value; }
        public Queue<AModBusMessage> ModBusCommand { get; } = new();
        public Queue<byte[]> Result { get; } = new();
        public bool Locked { get; set; }
        public Action? ActionAfterAnalysis { get => _actionAfterAnalysis; set => _actionAfterAnalysis = value; }
        public ModBusServerBase? ModBusServer { get => _modBusServer; set => _modBusServer = value; }
        public bool Reading { get => _reading; set => _reading = value; }
        public PlcServerBase? PlcServer { get => _plcServer; set => _plcServer = value; }
        public APlcClient? PlcTcpClient { get => _plcTcpClient; set => _plcTcpClient = value; }
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
        protected override void RunTask() {
            Task.Run(async () => {
                try {
                    while (Connected) {
                        // If is ModBus server
                        if (_modBusServer != null) {
                            // Check if any command
                            if (ModBusCommand.Count <= 0) {
                                if (_reading) {
                                    ReadResponse.SourceData = SendCommandToModBusServer(ReadRequest);

                                    byte[] bytes = ReadResponse.MessageData.Skip(ReadResponse.LengthOfSymbols).ToArray();
                                    _modBusServer.LoadData(bytes);
                                }
                            } else {
                                AModBusMessage req = ModBusCommand.Dequeue();
                                Result.Enqueue(SendCommandToModBusServer(req));
                            }
                        }
                        // If is plc
                        else if (_plcServer != null) {
                            // No need to have this, will use specific methods from specific server
                            // if (_reading) {
                            //     _plcServer.ReadBytes();
                            // }
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
                if (socketClient != null) {
                    socketClient.Close();
                }
            }
            CloseConnectionManually = true;
            ModBusCommand.Clear();
            Result.Clear();
        }
        public override bool WorkplaceCheckConnection() => Connected;
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

            // 1. check ping
            pingSuccess = MainUtils.PingHost(_ip);
            if (pingSuccess) {
                // 2. check socket
                try {
                    if (_communicationType is CommunicationSiemensPlc) {
                        MainUtils.Info(logger, $"COMMUNICATION[{_device_name} - {_ip}: {_port}] is Siemens PLC, ping is ok...");
                        socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socketClient.ReceiveTimeout = ReceiveTimeout;
                        socketClient.Connect(IPAddress.Parse(_ip), _port);
                        connectSuccess = true;
                    } else if (_communicationType is CommunicationModBusTcp) {
                        MainUtils.Info(logger, $"COMMUNICATION[{_device_name} - {_ip}: {_port}] is ModBus Tcp, ping is ok...");
                        connectSuccess = true;
                    } else {
                        socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socketClient.ReceiveTimeout = ReceiveTimeout;
                        socketClient.Connect(IPAddress.Parse(_ip), _port);
                        connectSuccess = true;
                        MainUtils.Info(logger, $"Successfully connect to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}]: {e}");
                }
            } else {
                logger.Warn($"Failed to ping COMMUNICATION[{_device_name} - {_ip}: {_port}]");
            }
            return pingSuccess && connectSuccess;
        }
        public byte[] SendCommandToModBusServer(AModBusMessage command) {
            if (Connected) {
                try {
                    // Send command to controller
                    socketClient.Send(command.MessageData);

                    // Receive data
                    byte[] msgBytes = new byte[1024 * 1024];
                    int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                    return msgBytes.Take(msgLen).ToArray();
                } catch (Exception e) {
                    logger.Error($"Error while sending command[{command.MessageDataString}] to COMMUNICATION[{_device_name} - {_ip}: {_port}], e: {e}");
                    // throw e;
                }
            }
            return new byte[0];
        }
        public async Task<byte[]> SendCommandToModBusServerAsync(AModBusMessage command) {
            return await Task.Run(() => SendCommandToModBusServer(command));
        }
        public async Task<byte[]> WriteToModBusServer(AModBusMessage req) {
            ModBusCommand.Enqueue(req);
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
