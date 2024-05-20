using System.Net;
using System.Net.Sockets;
using System.Text;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AsbtractClasses;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class ToolTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(ToolTask));

        #region Fields
        private static readonly object SendSyncRoot = new();
        private static readonly object ReceiveSyncRoot = new();
        private readonly int ReceiveTimeout = 2000;
        private readonly int HeartBeatDelay = 5000;
        private readonly int LockDelay = 1000;
        private bool? Locked = null;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeTool _toolType;
        private int HeartBeatCounter;
        private Action<TighteningData, int>? _actionAfterAnalysis;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually;
        // Other properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public DeviceTypeTool ToolType { get => _toolType; set => _toolType = value; }
        public string? Result { get; set; }
        public Action<TighteningData, int>? ActionAfterAnalysis { get => _actionAfterAnalysis; set => _actionAfterAnalysis = value; }
        #endregion

        #region Constructors
        public ToolTask(int deviceId, string? name, string ip, int port, DeviceTypeTool tool, int? workstationId = null) : base(deviceId, workstationId) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _toolType = tool;
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
                        // Reset heart beat counter
                        if (HeartBeatCounter >= 5000) {
                            HeartBeatCounter = 0;

                            // Only check hart beat interval if heart beat command is not null
                            if (_toolType is ToolPFSeries toolPF && toolPF.COMMAND_HEART_ASCII != null) {
                                System.Console.WriteLine("Send heart beat command to keep alive...");

                                // Send heart beat command to controller
                                string? result = await SendCommandAsync(toolPF.COMMAND_HEART_ASCII.GetMessage());

                                // If result is not equal to '9999' then the connection is down, break out of the loop
                                if (string.IsNullOrEmpty(result) || toolPF.GetMidFromResult(result) != "9999") {
                                    break;
                                }
                            }
                        }

                        // Looping interval
                        await Task.Delay(LoopingInterval);
                        HeartBeatCounter += LoopingInterval;
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while running heart beating task for connection<TOOL[{_device_name} - {_ip}: {_port}]>, e: {e}");
                } finally {
                    logger.Info($"Disconnected to TOOL[{_device_name} - {_ip}: {_port}]");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                    if (CloseConnectionManually) {
                        logger.Info($"Socket connection<TOOL[{_device_name} - {_ip}: {_port}]> has been closed manually, won't try to reconnecte anymore.");
                    }
                }
            });
            Task.Run(async () => {
                try {
                    while (Connected) {
                        try {
                            lock (ReceiveSyncRoot) {
                                byte[] msgBytes = new byte[1024 * 1024];
                                int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.Peek);
                                string resultTemp = Encoding.ASCII.GetString(msgBytes);

                                if (_toolType is ToolPFSeries toolPF) {
                                    Result = _toolType.AnalyzeData(resultTemp, _actionAfterAnalysis, DeviceId);
                                    // If result is not null, then this is not the result of tightening data, then keep don't retrieve it
                                    if (Result == null) {
                                        msgBytes = new byte[1024 * 1024];
                                        msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                                    }
                                } else {
                                    throw new Exception($"Tool[{_device_name} - {_ip}: {_port}] has no data handler set up, please check code.");
                                }
                            }

                            // Looping interval
                            await Task.Delay(LoopingInterval);
                        } catch (SocketException se) {
                            System.Console.WriteLine($"No data received... ");
                        } catch (Exception e) {
                            logger.Error($"Error while running task of waiting for tightening data for connection<TOOL[{_device_name} - {_ip}: {_port}]>: {e}");
                        }
                    }
                } finally {
                    logger.Warn($"Disconnected while waiting responses for connection<TOOL[{_device_name} - {_ip}: {_port}]>...");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                }
            });

            // Looping for checking lock status
            Task.Run(async () => {
                while (true) {
                    Locked = null;
                    await Task.Delay(LockDelay);
                }
            });
        }

        public override async void Connect() {
            HeartBeatCounter = HeartBeatDelay;
            CloseConnectionManually = false;
            while (!Connected) {
                Status = CONNECTING;
                if (ConnectToServer()) {
                    RegisterTCPClient();
                    RunTask();
                    Status = CONNECTED;
                    // Lock tool to keep safe
                    SendLock();
                    break;
                }
                await Task.Delay(AutoReconnectingTrialDelay);
            }
        }
        public override Task ConnectAsync() => Task.Run(() => Connect());
        public override void CloseConnection() {
            logger.Info($"Close connection<TOOL[{_device_name} - {_ip}: {_port}]> manually...");
            if (Connected) {
                socketClient.Close();
                DeregisterTCPClient();
            }
            CloseConnectionManually = true;
            Result = null;
        }
        public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        #endregion

        #region Methods
        private bool ConnectToServer() {
            if (Connected) {
                logger.Warn($"Already connecting to TOOL[{_device_name} - {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            logger.Info($"Connecting to TOOL[{_device_name} - {_ip}: {_port}]");
            bool pingSuccess = false;
            bool connectSuccess = false;
            bool sendConnectMsgSuceess = false;
            bool enableMsgSuccess = false;

            // 0. Check if socket already registerd
            Socket? socket = MainUtils.GetTCPClient(_ip, _port);
            if (socket != null) {
                socketClient = socket;
                MainUtils.Info(logger, $"Successfully connect to TOOL[{_device_name} - {_ip}: {_port}]");
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

                    // 3. send connecting message
                    if (connectSuccess && _toolType is ToolPFSeries toolPF) {
                        if (toolPF.COMMAND_CONNECT_ASCII != null) {
                            string? result1 = SendCommand(toolPF.COMMAND_CONNECT_ASCII.GetMessage());
                            if (result1 != null) {
                                string mid1 = toolPF.GetMidFromResult(result1);
                                sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";
                            }

                            // 4. send data receving enable message
                            if (sendConnectMsgSuceess && _toolType.COMMAND_DATA_ASCII != null) {
                                string? result2 = SendCommand(_toolType.COMMAND_DATA_ASCII.GetMessage());
                                if (result2 != null) {
                                    string mid2 = toolPF.GetMidFromResult(result2);
                                    enableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                                    if (enableMsgSuccess) {
                                        MainUtils.Info(logger, $"Successfully connect to TOOL[{_device_name} - {_ip}: {_port}]");
                                    }
                                }
                            }
                        }
                    } else {
                        sendConnectMsgSuceess = true;
                        enableMsgSuccess = true;
                    }
                } catch (Exception e) {
                    logger.Warn($"Connect error while connecting to TOOL[{_device_name} - {_ip}: {_port}], e: {e}");
                }
            } else {
                logger.Warn($"Failed to connect to TOOL[{_device_name} - {_ip}: {_port}]");
            }
            if (!(pingSuccess && connectSuccess && sendConnectMsgSuceess && enableMsgSuccess)) {
                if (socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip)) {
                    socketClient.Close();
                }
            }
            return pingSuccess && connectSuccess && sendConnectMsgSuceess && enableMsgSuccess;
        }
        public string? SendCommand(string command) {
            if (Connected) {
                try {
                    lock (SendSyncRoot) {
                        HeartBeatCounter = 0; // Reset heart beat counter to prevent multiple response
                        // Send command to controller
                        socketClient.Send(Encoding.ASCII.GetBytes(command));

                        // Receive data
                        lock (ReceiveSyncRoot) {
                            byte[] msgBytes = new byte[1024 * 1024];
                            int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                            string resultTemp = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                            string? result = _toolType.AnalyzeData(resultTemp, _actionAfterAnalysis, DeviceId);
                            Result = result;
                            return result;
                        }
                    }
                } catch (Exception e) {
                    logger.Error($"Error while sending command[{command}] to Tool[{_device_name} - {_ip}: {_port}], e: {e}");
                    // throw e;
                }
            }
            return null;
        }
        public async Task<string?> SendCommandAsync(string command) {
            return await Task<string?>.Run(() => SendCommand(command));
        }
        public async Task<bool> SendPSetAsync(int pSetNumber) {
            return await Task<bool>.Run(async () => {
                if (pSetNumber <= 0 || pSetNumber >= 999) {
                    WidgetUtils.ShowErrorPopUp("程序号范围必须在 0 ~ 999 以内！");
                } else if (_toolType.COMMAND_PSET_ASCII != null) {
                    string command = _toolType.COMMAND_PSET_ASCII.GetMessage(pSetNumber.ToString("000"));
                    string? result = await SendCommandAsync(command);

                    bool isOk;
                    if (result != null && _toolType is ToolPFSeries toolPF) {
                        isOk = toolPF.GetMidFromResult(result) == "0005";
                    } else if (result != null && _toolType is ToolSudongX7 toolSDX7) {
                        isOk = toolSDX7.SendPsetOk(result);
                    } else {
                        isOk = false;
                    }

                    return isOk;
                }
                return false;
            });
        }
        public async void SendLock() {
            if (_toolType.COMMAND_LOCK_ASCII != null && (Locked == null || !Locked.Value)) {
                string command = _toolType.COMMAND_LOCK_ASCII.GetMessage();
                string? result = await SendCommandAsync(command);

                bool isOk;
                if (result != null && _toolType is ToolPFSeries toolPF) {
                    isOk = toolPF.GetMidFromResult(result) == "0005";
                } else if (result != null && _toolType is ToolSudongX7 toolSDX7) {
                    isOk = true;
                } else {
                    isOk = false;
                }

                if (isOk) {
                    logger.Info($"Send lock successfully for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
                    Locked = true;
                    return;
                }
                logger.Warn($"Send lock failed for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
            }
        }
        public async Task<bool> SendLockAsync() {
            if (_toolType.COMMAND_LOCK_ASCII != null && (Locked == null || !Locked.Value)) {
                string command = _toolType.COMMAND_LOCK_ASCII.GetMessage();
                string? result = await SendCommandAsync(command);

                bool isOk;
                if (result != null && _toolType is ToolPFSeries toolPF) {
                    isOk = toolPF.GetMidFromResult(result) == "0005";
                } else if (result != null && _toolType is ToolSudongX7 toolSDX7) {
                    isOk = true;
                } else {
                    isOk = false;
                }

                if (isOk) {
                    logger.Info($"Send lock successfully for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
                    Locked = true;
                    return true;
                }
                logger.Warn($"Send lock async failed for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
            }
            return false;
        }
        public async void SendUnlock() {
            if (_toolType.COMMAND_UNLOCK_ASCII != null && (Locked == null || Locked.Value)) {
                string command = _toolType.COMMAND_UNLOCK_ASCII.GetMessage();
                string? result = await SendCommandAsync(command);

                bool isOk;
                if (result != null && _toolType is ToolPFSeries toolPF) {
                    isOk = toolPF.GetMidFromResult(result) == "0005";
                } else if (result != null && _toolType is ToolSudongX7 toolSDX7) {
                    isOk = true;
                } else {
                    isOk = false;
                }

                if (isOk) {
                    logger.Info($"Send unlock successfully for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
                    Locked = false;
                    return;
                }
                logger.Warn($"Send unlock failed for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
            }
        }
        public async Task<bool> SendUnlockAsync() {
            if (_toolType.COMMAND_UNLOCK_ASCII != null && (Locked == null || Locked.Value)) {
                string command = _toolType.COMMAND_UNLOCK_ASCII.GetMessage();
                string? result = await SendCommandAsync(command);

                bool isOk;
                if (result != null && _toolType is ToolPFSeries toolPF) {
                    isOk = toolPF.GetMidFromResult(result) == "0005";
                } else if (result != null && _toolType is ToolSudongX7 toolSDX7) {
                    isOk = true;
                } else {
                    isOk = false;
                }

                if (isOk) {
                    logger.Info($"Send unlock successfully for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
                    Locked = false;
                    return true;
                }
                logger.Warn($"Send unlock async failed for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
            }
            return false;
        }
        #endregion
    }
}
