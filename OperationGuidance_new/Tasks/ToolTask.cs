using System.Net;
using System.Net.Sockets;
using System.Text;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Tasks {
    public class ToolTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(ToolTask));

        #region Fields
        private readonly object SendSyncRoot = new();
        private readonly object ReceiveSyncRoot = new();
        private readonly int ReceiveTimeout = 2000;
        private readonly int HeartBeatDelay = 5000;
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
        public bool Locked { get; set; }
        public Action<TighteningData, int>? ActionAfterAnalysis { get => _actionAfterAnalysis; set => _actionAfterAnalysis = value; }
        #endregion

        #region Constructors
        public ToolTask(int deviceId, string? name, string ip, int port, DeviceTypeTool tool, int? workstationId = null) : base(deviceId, workstationId) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _toolType = tool;
            Locked = false;
            Status = DISCONNECTED;
        }
        #endregion

        #region Override methods
        protected override void RunTask() {
            Task.Run(async () => {
                try {
                    while (Connected) {
                        if (_toolType.COMMAND_HEART_ASCII != null && HeartBeatCounter >= 5000) {
                            HeartBeatCounter = 0;
                            System.Console.WriteLine("Send heart beat command to keep alive...");
                            // Send heart beat command to controller
                            string? result = await SendCommandAsync(_toolType.COMMAND_HEART_ASCII.GetMessage());
                            if (string.IsNullOrEmpty(result) || _toolType.GetMidFromResult(result) != "9999") {
                                break;
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
            Task.Run(() => {
                try {
                    while (Connected) {
                        try {
                            lock (ReceiveSyncRoot) {
                                byte[] msgBytes = new byte[1024 * 1024];
                                int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.Peek);
                                string resultTemp = Encoding.ASCII.GetString(msgBytes);
                                Result = _toolType.AnalyzeData(resultTemp, _actionAfterAnalysis, DeviceId);
                                if (Result == null) {
                                    msgBytes = new byte[1024 * 1024];
                                    msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                                }
                            }
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
        }

        public override Task Connect() {
            HeartBeatCounter = HeartBeatDelay;
            CloseConnectionManually = false;
            return Task.Run(async () => {
                while (!Connected) {
                    Status = CONNECTING;
                    if (ConnectToServer()) {
                        RunTask();
                        Status = CONNECTED;
                        // Lock tool to keep safe
                        SendLock();
                        break;
                    }
                    await Task.Delay(AuotReconnectingTrialDelay);
                }
            });
        }
        public override void CloseConnection() {
            logger.Info($"Close connection<TOOL[{_device_name} - {_ip}: {_port}]> manually...");
            if (Connected) {
                socketClient.Close();
            }
            CloseConnectionManually = true;
            Result = null;
        }
        public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        #endregion

        #region Methods
        // private async Task<string?> CheckResultAsync() {
        //     string? result = null;
        //     if (Connected) {
        //         byte[] msgBytes = new byte[1024 * 1024];
        //         int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.Peek);
        //         string resultTemp = Encoding.ASCII.GetString(msgBytes);
        //         result = _toolType.AnalyzeData(resultTemp, _actionAfterAnalysis);
        //     }
        //     return result;
        // }
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
                    if (connectSuccess && _toolType.COMMAND_CONNECT_ASCII != null) {
                        string? result1 = SendCommand(_toolType.COMMAND_CONNECT_ASCII.GetMessage());
                        if (result1 != null) {
                            string mid1 = _toolType.GetMidFromResult(result1);
                            sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";
                        }

                        // 4. send data receving enable message
                        if (sendConnectMsgSuceess && _toolType.COMMAND_DATA_ASCII != null) {
                            string? result2 = SendCommand(_toolType.COMMAND_DATA_ASCII.GetMessage());
                            if (result2 != null) {
                                string mid2 = _toolType.GetMidFromResult(result2);
                                enableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                                if (enableMsgSuccess) {
                                    MainUtils.Info(logger, $"Successfully connect to TOOL[{_device_name} - {_ip}: {_port}]");
                                }
                            }
                        }
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
                            Result = _toolType.AnalyzeData(resultTemp, _actionAfterAnalysis, DeviceId);
                            return Result;
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
                    return result != null && _toolType.GetMidFromResult(result) == "0005";
                }
                return false;
            });
        }
        public async void SendLock() {
            if (_toolType.COMMAND_LOCK_ASCII != null && !Locked) {
                string command = _toolType.COMMAND_LOCK_ASCII.GetMessage();
                string? result = await SendCommandAsync(command);
                if (result != null && _toolType.GetMidFromResult(result) == "0005") {
                    Locked = true;
                    return;
                }
                logger.Warn($"Send lock failed for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
            }
        }
        public async Task<bool> SendLockAsync() {
            if (_toolType.COMMAND_LOCK_ASCII != null && !Locked) {
                string command = _toolType.COMMAND_LOCK_ASCII.GetMessage();
                string? result = await SendCommandAsync(command);
                if (result != null && _toolType.GetMidFromResult(result) == "0005") {
                    Locked = true;
                    return true;
                }
                logger.Warn($"Send lock async failed for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
            }
            return false;
        }
        public async void SendUnlock() {
            if (_toolType.COMMAND_UNLOCK_ASCII != null && Locked) {
                string command = _toolType.COMMAND_UNLOCK_ASCII.GetMessage();
                string? result = await SendCommandAsync(command);
                if (result != null && _toolType.GetMidFromResult(result) == "0005") {
                    Locked = false;
                    return;
                }
                logger.Warn($"Send unlock failed for Tool[{_device_name} - {_ip}: {_port}], command = {command}, result = {result}");
            }
        }
        public async Task<bool> SendUnlockAsync() {
            if (_toolType.COMMAND_UNLOCK_ASCII != null && Locked) {
                string command = _toolType.COMMAND_UNLOCK_ASCII.GetMessage();
                string? result = await SendCommandAsync(command);
                if (result != null && _toolType.GetMidFromResult(result) == "0005") {
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
