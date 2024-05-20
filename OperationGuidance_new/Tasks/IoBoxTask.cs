using System.Net;
using System.Net.Sockets;
using log4net;
using OperationGuidance_new.Tasks.AsbtractClasses;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class IoBoxTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(IoBoxTask));

        #region Fields
        private static readonly object SendSyncRoot = new();
        private static readonly object ReceiveSyncRoot = new();
        private readonly int ReceiveTimeout = 2000;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually;
        public new int DeviceId { private get => base.DeviceId; set { } }
        // Other properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public bool Locked { get; set; }
        public IoBoxTypeArm? ArmType { get; set; }
        public IoBoxTypeArranger? ArrangerType { get; set; }
        public IoBoxTypeSetterSelector? SetterSelectorType { get; set; }
        #endregion

        #region Constructors
        public IoBoxTask(string ip, int port) : base(-1, null) {
            _ip = ip;
            _port = port;
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
                        // Check arm
                        if (ArmType != null && ArmType.RetrieveResult) {
                            try {
                                // Send and get response from TCP server if action is not null
                                if (ArmType.ActionAfterCoordinatesReceived != null) {
                                    string x = SendCommand(ArmType.DeviceType.COMMAND_READ_X_HEX.GetMessage());
                                    string y = SendCommand(ArmType.DeviceType.COMMAND_READ_Y_HEX.GetMessage());
                                    string? z = null;
                                    if (ArmType.DeviceType.COMMAND_READ_Z_HEX != null) {
                                        z = SendCommand(ArmType.DeviceType.COMMAND_READ_Z_HEX.GetMessage());
                                    }
                                    // logger.Debug($"[_ioBoxType.Name:{ArmType.DeviceType.Name}] result: x = {x}, y = {y}, z = {z}");

                                    // Analyze data
                                    ArmType.DeviceType.AnalyzeData(x, y, z, ArmType.ActionAfterCoordinatesReceived, ArmType.DeviceId);
                                }
                            } catch (Exception e) {
                                logger.Warn($"Exception has been thrown while sending and getting coordinates from _ioBoxType.Name:{ArmType.DeviceType.Name}], e = {e}");
                            }
                        }

                        // Check arranger
                        if (ArrangerType != null && ArrangerType.RetrieveResult) {
                            if (ArrangerType.ActionAfterCoordinatesReceived != null) {
                                try {
                                    string readResult = SendCommand(ArrangerType.DeviceType.COMMAND_READ.GetMessage());
                                    logger.Debug($"[_ioBoxType.Name:{ArrangerType.DeviceType.Name}] result: readResult = {readResult}");

                                    // Analyze data
                                    ArrangerType.DeviceType.AnalyzeData(readResult, ArrangerType.ActionAfterCoordinatesReceived);
                                } catch (Exception e) {
                                    logger.Warn($"Exception has been thrown while reading from _ioBoxType.Name:{ArrangerType.DeviceType.Name}], e = {e}");
                                }
                            }
                        }

                        // Check setter selector
                        if (SetterSelectorType != null && SetterSelectorType.RetrieveResult) {
                            if (SetterSelectorType.ActionAfterCoordinatesReceived != null) {
                                try {
                                    string readResult = SendCommand(SetterSelectorType.DeviceType.COMMAND_READ.GetMessage());
                                    logger.Debug($"[_ioBoxType.Name:{SetterSelectorType.DeviceType.Name}] result: readResult = {readResult}");

                                    // Analyze data
                                    SetterSelectorType.DeviceType.AnalyzeData(readResult, SetterSelectorType.ActionAfterCoordinatesReceived);
                                } catch (Exception e) {
                                    logger.Warn($"Exception has been thrown while reading from _ioBoxType.Name:{SetterSelectorType.DeviceType.Name}], e = {e}");
                                }
                            }
                        }

                        // Common delay
                        await Task.Delay(LoopingInterval);
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while running task for connection<IOBOX[ {_ip}: {_port}]>: {e}");
                } finally {
                    logger.Info($"Disconnected to IOBOX[ {_ip}: {_port}]");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                    if (CloseConnectionManually) {
                        logger.Info($"Socket connection<IOBOX[ {_ip}: {_port}]> has been closed manually, won't try to reconnecte anymore.");
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
            logger.Info($"Close connection<IOBOX[ {_ip}: {_port}]> manually...");
            if (Connected) {
                socketClient.Close();
                DeregisterTCPClient();
            }
            CloseConnectionManually = true;
        }
        public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        #endregion

        #region Methods
        private bool ConnectToServer() {
            if (Connected) {
                logger.Warn($"Already connecting to IOBOX[ {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            logger.Info($"Connecting to IOBOX[ {_ip}: {_port}]");
            bool pingSuccess = false;
            bool connectSuccess = false;

            // 0. Check if socket already registerd
            Socket? socket = MainUtils.GetTCPClient(_ip, _port);
            if (socket != null) {
                socketClient = socket;
                MainUtils.Info(logger, $"Successfully connect to IOBOX[ {_ip}: {_port}]");
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
                    MainUtils.Info(logger, $"Successfully connect to IOBOX[ {_ip}: {_port}]");
                } catch (Exception e) {
                    logger.Warn($"Error while connecting to IOBOX[ {_ip}: {_port}]: {e}");
                }
            } else {
                logger.Warn($"Failed to ping IOBOX[ {_ip}: {_port}]");
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
                    logger.Error($"Error while sending command[{command}] to IOBOX[ {_ip}: {_port}], e: {e}");
                    // throw e;
                }
            }
            return "";
        }
        public async Task<string> SendCommandAsync(string command) {
            return await Task.Run(() => SendCommand(command));
        }
        #endregion
    }
}
