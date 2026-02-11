using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;
using OperationGuidance_new.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OperationGuidance_new.Tasks {
    public class ToolTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(ToolTask));

        #region Fields
        private static readonly object SyncObject = new();
        private readonly int SendMessageRecevingTimes = 5;
        private readonly int ReceiveTimeout = 200;
        private readonly int HeartBeatDelay = 5000;
        private readonly int PSetWaitTime = 300;
        private readonly int LockMaxTimes = 2;
        private readonly int UnLockMaxTimes = 2;
        private readonly int LockWaitTime = 500;
        private int SendMessageRecevingCount = 0;
        private volatile bool _locked = false;
        private readonly object _pSetLock = new object();
        private volatile int _sendingPSet = -1;
        private volatile int _currentPSet = -1;
        private volatile PSetStatus _pSetStatus = PSetStatus.NONE;
        private readonly SemaphoreSlim _pSetSem = new(1, 1);
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeTool _toolType;
        private int HeartBeatCounter;
        private int LockCounter;
        private int UnLockCounter;
        private int LockWaitTimeCounter;
        private Action<TighteningData, int>? _actionAfterAnalysis;
        private Action<CurveDataTemp, int>? _actionAfterCurveDataReceived;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually;
        // Other properties
        public bool Locked => _locked;
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public DeviceTypeTool ToolType { get => _toolType; set => _toolType = value; }
        public Action<TighteningData, int>? ActionAfterAnalysis { get => _actionAfterAnalysis; set => _actionAfterAnalysis = value; }
        public Action<CurveDataTemp, int>? ActionAfterCurveDataReceived { get => _actionAfterCurveDataReceived; set => _actionAfterCurveDataReceived = value; }
        public int CurrentPSet {
            get { lock (_pSetLock) return _currentPSet; }
            set { lock (_pSetLock) _currentPSet = value; }
        }
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
        protected override void RunTask() {
            Task.Run(async () => {
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Task thread started");
                try {
                    while (Connected) {
                        // Check if it's time to send heart beating command
                        if (HeartBeatCounter >= HeartBeatDelay) {
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Heartbeat timer reached threshold (counter={HeartBeatCounter}ms, threshold={HeartBeatDelay}ms)");
                            // Only check hart beat interval if heart beat command is not null
                            if (_toolType is ToolPFSeries toolPF && toolPF.COMMAND_HEART_ASCII != null) {
                                // Send heart beat command to controller
                                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Sending heartbeat command (ToolType={_toolType.GetType().Name})");
                                bool sendResult = SendCommand(toolPF.COMMAND_HEART_ASCII.GetMessage());
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Heartbeat command send result: {sendResult}");
                            } else {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] No heartbeat command configured for tool type {_toolType.GetType().Name}");
                            }
                            // Reset heart beat counter even no command has been sent
                            HeartBeatCounter = 0;
                        }

                        // Check any message is waiting for receving
                        try {
                            byte[] msgBytes = new byte[1024 * 1024];
                            int msgLen = 0;
                            lock (SyncObject) {
                                msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                            }
                            if (msgLen > 0) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Received data, length={msgLen} bytes");
                                AnalyzeData(msgBytes.Take(msgLen).ToArray());
                            }
                        } catch (SocketException se) {
                            if (se.ErrorCode == (int) SocketError.TimedOut) {
                                HeartBeatCounter += ReceiveTimeout;
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Socket receive timeout (ErrorCode={se.ErrorCode}), incrementing heartbeat counter by {ReceiveTimeout}ms");
                            } else {
                                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Socket exception during receive", se);
                                throw;
                            }
                        }

                        // Check for lock wait time
                        if (LockWaitTimeCounter >= LockWaitTime) {
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Lock wait time reached threshold (counter={LockWaitTimeCounter}ms, threshold={LockWaitTime}ms), resetting counters");
                            LockWaitTimeCounter = 0;
                            LockCounter = 0;
                            UnLockCounter = 0;
                        }

                        // Looping interval
                        await Task.Delay(LoopingInterval);
                        HeartBeatCounter += LoopingInterval;
                        LockWaitTimeCounter += LoopingInterval;
                    }
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Main loop exited, connection status: Connected={Connected}");
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Fatal error in task loop", e);
                } finally {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Task thread terminating, cleaning up resources");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Socket closed and set to null");
                    }
                    if (CloseConnectionManually) {
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection closed manually, will not attempt reconnection");
                    }
                }
            });

            void AnalyzeData(byte[] msgBytes) {
                try {
                    string dataPreview = msgBytes.Length > 100 ? $"{string.Join(", ", msgBytes.Take(100))}...(truncated)" : string.Join(", ", msgBytes);
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Analyzing received data (length={msgBytes.Length} bytes): [{dataPreview}]");

                    // Analyse result
                    if (_toolType is ToolPFSeries toolPF2) {
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Processing data with ToolPFSeries analyzer");
                        toolPF2.AnalyzeData(msgBytes, (Action<bool?, bool?, bool?, bool?, bool?>) (async (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            await Task.Run((Action) (() => {
                                if (heartIsBeating != null) {
                                    if (!heartIsBeating.Value) {
                                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Heartbeat validation failed - heartbeat is not detected");
                                        throw new Exception("Heart is not beating...");
                                    } else {
                                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Heartbeat validation successful");
                                    }
                                }
                                if (pSetSendingOk != null && pSetSendingOk.HasValue) {
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status update: CurrentStatus={_pSetStatus}, NewValue={pSetSendingOk.Value}");
                                    if (_pSetStatus == PSetStatus.NONE) {
                                        if (pSetSendingOk.Value) {
                                            _pSetStatus = PSetStatus.OK;
                                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status changed: NONE -> OK");
                                        } else {
                                            _pSetStatus = PSetStatus.NOK;
                                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status changed: NONE -> NOK");
                                        }
                                    } else {
                                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status unchanged (current={_pSetStatus})");
                                    }
                                }
                                if (locked != null) {
                                    bool oldLocked = _locked;
                                    _locked = locked.Value;
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Lock state changed: {oldLocked} -> {_locked}");
                                }
                                if (dataReceived != null && dataReceived.Value) {
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received flag set");
                                }
                                if (curveReceived != null && curveReceived.Value) {
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Curve data received, sending ACK");
                                    socketClient.Send(Encoding.ASCII.GetBytes(toolPF2.COMMAND_CURVE_ACK_ASCII.GetMessage()));
                                }
                            }));
                        }), _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Processing data with ToolSudongX7 analyzer");
                        toolX7.AnalyzeData(msgBytes, (Action<bool?, bool?, bool?, bool?, bool?>) (async (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            await Task.Run((Action) (() => {
                                if (pSetSendingOk != null && pSetSendingOk.HasValue) {
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status update: CurrentStatus={_pSetStatus}, NewValue={pSetSendingOk.Value}");
                                    if (_pSetStatus == PSetStatus.NONE) {
                                        if (pSetSendingOk.Value) {
                                            _pSetStatus = PSetStatus.OK;
                                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status changed: NONE -> OK");
                                        } else {
                                            _pSetStatus = PSetStatus.NOK;
                                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status changed: NONE -> NOK");
                                        }
                                    } else {
                                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status unchanged (current={_pSetStatus})");
                                    }
                                }
                                if (locked != null) {
                                    bool oldLocked = _locked;
                                    _locked = locked.Value;
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Lock state changed: {oldLocked} -> {_locked}");
                                }
                                if (dataReceived != null && dataReceived.Value) {
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received flag set");
                                }
                                if (curveReceived != null && curveReceived.Value) {
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Curve data received flag set");
                                }
                            }));
                        }), _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
                    } else {
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type: {_toolType.GetType().Name}");
                    }
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Error during data analysis", e);
                }
            }
        }

        public override void Connect() {
            lock (SyncObject) {
                Task.Run(async () => {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Initiating connection process");
                    HeartBeatCounter = 0;
                    CloseConnectionManually = false;

                    int retryCount = 0;
                    while (!Connected) {
                        retryCount++;
                        Status = CONNECTING;
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Connection attempt #{retryCount}, Status={Status}");

                        if (await ConnectToServer()) {
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection established successfully");
                            RunTask();
                            Status = CONNECTED;
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Status changed to CONNECTED");

                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending initial unlock command");
                            ForceSendUnlock();
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Initial unlock sent, breaking connection loop");
                            break;
                        }
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connection attempt #{retryCount} failed, retrying in {AutoReconnectingTrialDelay}ms");
                        await Task.Delay(AutoReconnectingTrialDelay);
                    }
                    if (Connected) {
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection process completed successfully after {retryCount} attempt(s)");
                    } else {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Connection process failed after {retryCount} attempt(s)");
                    }
                });
            }
        }
        public override Task ConnectAsync() => Task.Run(() => Connect());
        public override void CloseConnection() {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Close connection requested (manual close)");

            if (Connected) {
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Socket is connected, closing now...");
                socketClient.Close();
                socketClient = null;
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Socket closed and set to null");
            } else {
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Socket is already disconnected");
            }

            CloseConnectionManually = true;
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Manual close flag set to true");
        }
        // public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        public override bool WorkplaceCheckConnection() => Connected;
        #endregion

        #region Methods
        private async Task<bool> ConnectToServer() {
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] ConnectToServer() called");
            try {
                if (Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Already connected, aborting new connection attempt");
                    return false;
                }

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Starting connection sequence (IP={_ip}, Port={_port}, ToolType={_toolType.GetType().Name})");
                bool pingSuccess = false;
                bool connectSuccess = false;
                bool sendConnectMsgSuceess = false;
                bool dataEnableMsgSuccess = false;

                // 1. check ping
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Step 1: Pinging host {_ip}");
                pingSuccess = MainUtils.PingHost(_ip);
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Ping result: {pingSuccess}");
                if (pingSuccess) {
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Step 2: Creating TCP socket");
                    // 2. check socket
                    try {
                        socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socketClient.ReceiveTimeout = ReceiveTimeout;
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Connecting to {_ip}:{_port} (ReceiveTimeout={ReceiveTimeout}ms)");
                        socketClient.Connect(IPAddress.Parse(_ip), _port);
                        connectSuccess = true;
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Socket connection established successfully");

                        // 3. send connecting message
                        if (connectSuccess && _toolType is ToolPFSeries toolPF) {
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Step 3: Sending connection message (ToolPFSeries detected)");
                            if (toolPF.COMMAND_CONNECT_ASCII != null) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Step 3a: Sending connect command, SendMessageRecevingCount reset to 0");
                                SendMessageRecevingCount = 0;
                                string? result1 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_CONNECT_ASCII.GetMessage());
                                if (result1 != null) {
                                    string mid1 = toolPF.GetMid(result1);
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connect command response - MID: {mid1}, Full Response: {result1}");
                                    sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Connect command success: {sendConnectMsgSuceess} (accepted MIDs: 0002, 0005)");
                                } else {
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connect command - no response received");
                                    sendConnectMsgSuceess = false;
                                }

                                // 4. send data receving enable message
                                if (sendConnectMsgSuceess) {
                                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Step 4: Sending data enable message");
                                    SendMessageRecevingCount = 0;
                                    string? result2 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_DATA_ASCII.GetMessage());
                                    if (result2 != null) {
                                        string mid2 = toolPF.GetMid(result2);
                                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Data enable command response - MID: {mid2}, Full Response: {result2}");
                                        dataEnableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data enable command success: {dataEnableMsgSuccess} (accepted MIDs: 0002, 0005)");
                                    } else {
                                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Data enable command - no response received");
                                        dataEnableMsgSuccess = false;
                                    }

                                    // 5. send curve data receving enable message
                                    if (dataEnableMsgSuccess) {
                                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Step 5: Sending curve data enable message (optional)");
                                        // Don't need to check result, because if PF6000 doesn't have any license for curve data, then it will return 0004 which means it failed, it can not retrieve any curve data
                                        SendMessageRecevingCount = 0;
                                        var curveResult = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_CURVE_ASCII.GetMessage());
                                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Curve data enable sent, response: {curveResult ?? "null"}");
                                    } else {
                                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Skipping curve data enable due to previous failure");
                                    }
                                } else {
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Skipping data enable message due to connect command failure");
                                }
                            } else {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] No connect command configured, skipping connection message sequence");
                                sendConnectMsgSuceess = true;
                                dataEnableMsgSuccess = true;
                            }
                        } else {
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Tool type is not ToolPFSeries, skipping connection message sequence");
                            sendConnectMsgSuceess = true;
                            dataEnableMsgSuccess = true;
                        }
                    } catch (Exception e) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Socket connection or initialization error", e);
                    }
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Ping failed to host {_ip}");
                }
                bool isConnected = pingSuccess && connectSuccess && sendConnectMsgSuceess && dataEnableMsgSuccess;

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection sequence completed - Overall success: {isConnected}");
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Step results - Ping: {pingSuccess}, Socket: {connectSuccess}, ConnectMsg: {sendConnectMsgSuceess}, DataEnable: {dataEnableMsgSuccess}");

                if (isConnected) {
                    MainUtils.Info(logger, $"[TOOL:{_device_name}-{_ip}:{_port}] Successfully connected to tool");
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed, cleaning up resources");
                    if (socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip)) {
                        socketClient.Close();
                        socketClient = null;
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Failed socket connection closed and cleaned up");
                    }
                }
                return isConnected;
            } catch (Exception e) {
                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Unexpected error during connection", e);
            }

            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] ConnectToServer() returning false");
            return false;
        }
        private bool SendCommand(string command) {
            if (!Connected) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Command not sent - not connected: {command}");
                return false;
            }

            try {
                byte[] data = _toolType is ToolSudongX7
                                        ? MainUtils.ToBytes(command)
                                        : Encoding.ASCII.GetBytes(command);

                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending command (length={data.Length} bytes): {command}");
                int? num;
                lock (SyncObject) {
                    num = socketClient?.Send(data);
                }
                if (num.HasValue && num.Value > 0) {
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Command sent successfully, bytes sent: {num.Value}");
                    return true;
                }

                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Command sending failed: {command}, bytes sent = {num}");
            } catch (Exception ex) {
                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Command sending error: {command}", ex);
            }

            return false;
        }
        private async Task<string?> SendAndReceiveOnlyForPreparingAsync(string command) {
            SendMessageRecevingCount++;
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] SendAndReceive attempt #{SendMessageRecevingCount}/{SendMessageRecevingTimes} for command: {command}");

            if (Connected && SendMessageRecevingCount < SendMessageRecevingTimes) {
                try {
                    // Reset heart beat counter to prevent multiple response
                    HeartBeatCounter = 0;
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Heartbeat counter reset to 0 to prevent interference");

                    // Send command to controller
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Sending command: {command}");
                    socketClient.Send(Encoding.ASCII.GetBytes(command));
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Command sent, waiting for response...");

                    // Receive data
                    byte[] msgBytes = new byte[1024 * 1024];
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Waiting for response (timeout={ReceiveTimeout}ms)...");
                    int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                    string result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Received response (length={msgLen} bytes): {result}");
                    return result;
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Error during send/receive for command: {command}", e);
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Retrying send/receive (attempt #{SendMessageRecevingCount + 1})...");
                    return await SendAndReceiveOnlyForPreparingAsync(command);
                }
            } else {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] SendAndReceive aborted - not connected");
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] SendAndReceive aborted - max retries reached ({SendMessageRecevingCount}/{SendMessageRecevingTimes})");
                }
            }
            return null;
        }
        public async Task<bool> SendPSetAsync(int pSetNumber) {
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] SendPSetAsync() called with pSetNumber={pSetNumber}");
            if (pSetNumber == CurrentPSet) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet operation skipped - CurrentPSet={CurrentPSet} equals requested pSetNumber={pSetNumber}");
                return true;
            }

            if (!Connected) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet operation failed - not connected");
                return false;
            }

            // 互斥锁，保证一次只能发送一个 pset 命令
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Acquiring PSet semaphore lock");
            await _pSetSem.WaitAsync();
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet semaphore lock acquired");

            try {
                if (Connected) {
                    try {
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Starting PSet operation - CurrentPSet={CurrentPSet}, TargetPSet={pSetNumber}");
                        string command = "";
                        string toolName = "";
                        if (_toolType is ToolPFSeries toolPF) {
                            command = toolPF.GetPSetCommand(pSetNumber);
                            toolName = toolPF.Name;
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Generated PSet command for ToolPFSeries: {command}");
                        } else if (_toolType is ToolSudongX7 toolX7) {
                            command = toolX7.GetPSetCommand(pSetNumber);
                            toolName = toolX7.Name;
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Generated PSet command for ToolSudongX7: {command}");
                        } else {
                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type: {_toolType?.GetType().Name}");
                        }

                        // Send pset
                        if (string.IsNullOrEmpty(command)) {
                            logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] PSet operation failed - No command generated for pset {pSetNumber} with tool type {_toolType?.GetType().Name}");
                            return false;
                        }
                        int waitTimesMax = 15;
                        int waitTimes = 0;

                        // 设置等待状态，确保前面没有残留的状态
                        _pSetStatus = PSetStatus.NONE;
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status reset to NONE, starting wait loop (max attempts={waitTimesMax}, wait interval={PSetWaitTime}ms)");

                        while (_pSetStatus == PSetStatus.NONE && waitTimes < waitTimesMax) {
                            try {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet attempt #{waitTimes + 1}/{waitTimesMax} - Sending command: {command}");
                                bool sendResult = SendCommand(command);
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet command send result: {sendResult}");
                                waitTimes++;
                            } catch (Exception e) {
                                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Error while sending PSet command [{command}] (attempt #{waitTimes + 1})", e);
                            }

                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Waiting {PSetWaitTime}ms for PSet response (attempt #{waitTimes}/{waitTimesMax})");
                            await Task.Delay(PSetWaitTime);
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Wait complete, PSet status: {_pSetStatus}");
                        }

                        bool isSuccess;
                        if (_pSetStatus == PSetStatus.NONE) {
                            isSuccess = false;
                            _pSetStatus = PSetStatus.NOK;
                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet operation timed out - no response after {waitTimesMax} attempts");
                        } else {
                            isSuccess = _pSetStatus == PSetStatus.OK;
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet status final result: {_pSetStatus}");
                        }

                        if (isSuccess) {
                            int oldPSet = CurrentPSet;
                            CurrentPSet = pSetNumber;
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet operation successful - CurrentPSet updated: {oldPSet} -> {pSetNumber}");
                        } else {
                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet operation failed - Status: {_pSetStatus}");
                        }

                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet operation completed - Target={pSetNumber}, FinalStatus={_pSetStatus}, Success={isSuccess}");
                        return isSuccess;
                    } catch (Exception e) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Unexpected error during PSet operation to {pSetNumber}", e);
                        return false;
                    }
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet operation aborted - disconnected during operation");
                }
            } finally {
                _pSetSem.Release();
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] PSet semaphore lock released");
            }

            return false;
        }

        private void SendLock() {
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] SendLock() called");
            if (Connected) {
                if (LockCounter < LockMaxTimes) {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Locking tool (attempt #{LockCounter + 1}/{LockMaxTimes})...");
                    if (_toolType is ToolPFSeries toolPF) {
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending lock command to ToolPFSeries");
                        SendCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        if (!_locked) {
                            string cmd = toolX7.GetLockCommand();
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending lock command to ToolSudongX7 (current locked={_locked})");
                            SendCommand(cmd);
                            Thread.Sleep(500);
                            SendCommand(cmd);
                            _locked = true;
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ToolSudongX7 locked, _locked flag set to true");
                        } else {
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] ToolSudongX7 already locked, skipping lock commands");
                        }
                    } else {
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type for lock operation: {_toolType.GetType().Name}");
                    }

                    LockCounter++;
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Lock counter incremented: {LockCounter}");
                } else {
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Max lock attempts reached ({LockCounter}/{LockMaxTimes}), skipping lock");
                }
            } else {
                _locked = false;
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Lock operation failed - not connected");
            }
        }
        public void ForceSendLock() {
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] ForceSendLock() called");
            if (Connected) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force locking tool...");
                if (_toolType is ToolPFSeries toolPF) {
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending force lock command to ToolPFSeries (current locked={_locked})");
                    SendCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
                } else if (_toolType is ToolSudongX7 toolX7) {
                    if (!_locked) {
                        string cmd = toolX7.GetLockCommand();
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending force lock command to ToolSudongX7");
                        SendCommand(cmd);
                        Thread.Sleep(500);
                        SendCommand(cmd);
                        _locked = true;
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ToolSudongX7 force locked, _locked flag set to true");
                    } else {
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] ToolSudongX7 already locked, skipping lock commands");
                    }
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type for force lock operation: {_toolType.GetType().Name}");
                }
            } else {
                _locked = false;
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force lock operation failed - not connected");
            }
        }
        private void SendUnlock() {
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] SendUnlock() called");
            if (Connected) {
                if (UnLockCounter < UnLockMaxTimes) {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Unlocking tool (attempt #{UnLockCounter + 1}/{UnLockMaxTimes})...");
                    if (_toolType is ToolPFSeries toolPF) {
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending unlock command to ToolPFSeries");
                        SendCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        if (_locked) {
                            string cmd = toolX7.GetUnlockCommand();
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending unlock command to ToolSudongX7 (current locked={_locked})");
                            SendCommand(cmd);
                            Thread.Sleep(500);
                            SendCommand(cmd);
                            _locked = false;
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ToolSudongX7 unlocked, _locked flag set to false");
                        } else {
                            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] ToolSudongX7 already unlocked, skipping unlock commands");
                        }
                    } else {
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type for unlock operation: {_toolType.GetType().Name}");
                    }

                    UnLockCounter++;
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock counter incremented: {UnLockCounter}");
                } else {
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Max unlock attempts reached ({UnLockCounter}/{UnLockMaxTimes}), skipping unlock");
                }
            } else {
                _locked = true;
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock operation failed - not connected");
            }
        }
        public void ForceSendUnlock() {
            logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] ForceSendUnlock() called");
            if (Connected) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlocking tool...");
                if (_toolType is ToolPFSeries toolPF) {
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending force unlock command to ToolPFSeries (current locked={_locked})");
                    SendCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
                } else if (_toolType is ToolSudongX7 toolX7) {
                    if (_locked) {
                        string cmd = toolX7.GetUnlockCommand();
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Sending force unlock command to ToolSudongX7");
                        SendCommand(cmd);
                        Thread.Sleep(500);
                        SendCommand(cmd);
                        _locked = false;
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ToolSudongX7 force unlocked, _locked flag set to false");
                    } else {
                        logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] ToolSudongX7 already unlocked, skipping unlock commands");
                    }
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type for force unlock operation: {_toolType.GetType().Name}");
                }
            } else {
                _locked = true;
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlock operation failed - not connected");
            }
        }
        #endregion

        private enum PSetStatus {
            NONE,
            OK,
            NOK,
        }
    }
}
