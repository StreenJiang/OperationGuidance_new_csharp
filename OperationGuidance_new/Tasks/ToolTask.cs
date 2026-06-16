using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;
using OperationGuidance_new.Utils;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OperationGuidance_new.Tasks {
    public class ToolTask: ATaskBase {
        private enum PendingLockCommand { None, Lock, Unlock }

        private ILog logger = MainUtils.GetLogger(typeof(ToolTask));

        #region Fields
        private readonly object SyncObject = new();
        private readonly object LockSyncObject = new();
        private readonly object _sendLock = new();
        private readonly int SendMessageRecevingTimes = 5;
        private readonly int ReceiveTimeout = 200;
        private readonly int HeartBeatDelay = 5000;
        private readonly int PSetWaitTime = 200;
        private readonly int PSetWaitTimesMax = 5;
        private int SendMessageRecevingCount = 0;
        private Task? _runTaskTask;
        private volatile int _connectInProgress;
        private volatile bool _locked = false;
        private volatile bool _lockStatusSending = false;
        private readonly object _pSetLock = new object();
        private volatile int _sendingPSet = -1;
        private volatile int _currentPSet = -1;
        private volatile bool _psetSentOk = false;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeTool _toolType;
        private volatile int HeartBeatCounter;
        private volatile PendingLockCommand _pendingLockCommand = PendingLockCommand.None;
        private Action<TighteningData, int>? _actionAfterAnalysis;
        private Func<CurveDataTemp, int, Task>? _actionAfterCurveDataReceived;
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
        public Func<CurveDataTemp, int, Task>? ActionAfterCurveDataReceived { get => _actionAfterCurveDataReceived; set => _actionAfterCurveDataReceived = value; }
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
            _runTaskTask = Task.Run(async () => {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Task thread started");
                try {
                    while (Connected) {
                        // Only check hart beat interval if heart beat command is not null
                        if (_toolType is ToolPFSeries toolPF && toolPF.COMMAND_HEART_ASCII != null) {
                            // Check if it's time to send heart beating command
                            if (HeartBeatCounter >= HeartBeatDelay) {
                                // Send heart beat command to controller
                                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Sending heartbeat command");
                                SendCommand(toolPF.COMMAND_HEART_ASCII.GetMessage());
                            }
                        } else if (_toolType is ToolFITFTC6 toolFitFTC6) {
                            if (HeartBeatCounter >= toolFitFTC6.HEART_BEAT_PERIOD) {
                                // Send heart beat command to controller
                                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Sending heartbeat command");
                                SendCommand(toolFitFTC6.GetHeartBeatCommand());
                            }
                        }

                        // Check any message is waiting for receving
                        try {
                            byte[] msgBytes = new byte[1024 * 1024];
                            int msgLen = 0;
                            lock (SyncObject) {
                                msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                            }
                            if (msgLen > 0) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received, length={msgLen} bytes");
                                AnalyzeData(msgBytes.Take(msgLen).ToArray());
                            }
                        } catch (SocketException se) {
                            if (se.ErrorCode == (int) SocketError.TimedOut) {
                                HeartBeatCounter += ReceiveTimeout;
                            } else {
                                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Socket exception during receive", se);
                                throw;
                            }
                        }

                        // Looping interval
                        await Task.Delay(LoopingInterval);
                        HeartBeatCounter += LoopingInterval;
                    }
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Main loop exited, Connected={Connected}");
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Fatal error in task loop", e);
                } finally {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Task thread terminating");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                    if (CloseConnectionManually) {
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection closed manually");
                    }
                }
            });

            void AnalyzeData(byte[] msgBytes) {
                try {
                    // Analyse result
                    if (_toolType is ToolPFSeries toolPF2) {
                        toolPF2.AnalyzeData(msgBytes, (Action<bool?, bool?, bool?, bool?, bool?>) (async (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            if (heartIsBeating != null) {
                                if (!heartIsBeating.Value) {
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Heartbeat validation failed");
                                    throw new Exception("Heart is not beating...");
                                }
                            }
                            if (pSetSendingOk != null && _sendingPSet != -1) {
                                if (pSetSendingOk.HasValue) {
                                    _psetSentOk = pSetSendingOk.Value;
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet sending to {_sendingPSet} result: {_psetSentOk}");
                                }
                            }
                            if (locked != null && locked.HasValue) {
                                UpdateInternalLockState(locked.Value);
                            }
                            if (dataReceived != null && dataReceived.Value) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received");
                            }
                            if (curveReceived != null && curveReceived.Value) {
                                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Curve data received");
                                socketClient.Send(Encoding.ASCII.GetBytes(toolPF2.COMMAND_CURVE_ACK_ASCII.GetMessage()));
                            }
                        }), _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        toolX7.AnalyzeData(msgBytes, (Action<bool?, bool?, bool?, bool?, bool?>) (async (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            if (pSetSendingOk != null && _sendingPSet != -1) {
                                if (pSetSendingOk.HasValue) {
                                    _psetSentOk = pSetSendingOk.Value;
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet sending to {_sendingPSet} result: {_psetSentOk}");
                                }
                            }
                            if (dataReceived != null && dataReceived.Value) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received");
                            }
                            if (curveReceived != null && curveReceived.Value) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Curve data received");
                            }
                        }), _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
                    } else if (_toolType is ToolFITFTC6 toolFitFTC6) {
                        toolFitFTC6.AnalyzeData(msgBytes, (Action<bool?, bool?, bool?, bool?, bool?>) (async (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            if (heartIsBeating != null) {
                                if (!heartIsBeating.Value) {
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Heartbeat validation failed");
                                    throw new Exception("Heart is not beating...");
                                }
                            }
                            if (pSetSendingOk != null && _sendingPSet != -1) {
                                if (pSetSendingOk.HasValue) {
                                    _psetSentOk = pSetSendingOk.Value;
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet sending to {_sendingPSet} result: {_psetSentOk}");
                                }
                            }
                            if (locked != null && locked.HasValue) {
                                if (locked.Value) {
                                    UpdateInternalLockState(_lockStatusSending);
                                }
                            }
                            if (dataReceived != null && dataReceived.Value) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received");
                            }
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
            if (Interlocked.Exchange(ref _connectInProgress, 1) == 1) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connect already in progress, skipping");
                return;
            }
            Task.Run(async () => {
                try {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Initiating connection");
                    HeartBeatCounter = 0;
                    CloseConnectionManually = false;

                    int retryCount = 0;
                    while (!Connected) {
                        retryCount++;
                        Status = CONNECTING;

                        if (await ConnectToServer()) {
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection established");
                            _toolType.ClearResidual();  // 清除旧连接残留
                            RunTask();
                            Status = CONNECTED;
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Status: CONNECTED");

                            ForceSendUnlock();
                            break;
                        }
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed, retrying ({retryCount})");
                        await Task.Delay(AutoReconnectingTrialDelay);
                    }
                    if (Connected) {
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection completed after {retryCount} attempt(s)");
                    } else {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed after {retryCount} attempt(s)");
                    }
                } finally {
                    _connectInProgress = 0;
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connect task exiting, _connectInProgress released, Connected={Connected}");
                }
            });
        }
        public override Task ConnectAsync() => Task.Run(() => Connect());
        public override void CloseConnection() {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Closing connection (manual)");

            if (Connected) {
                socketClient.Close();
                socketClient = null;
                _toolType.ClearResidual();  // 清除残留
            }

            CloseConnectionManually = true;
        }
        public void CloseToTriggerReconnection() {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Closing connection to trigger reconnection...");
            _currentPSet = -1;  // 连接断开后缓存不可信，下次发送时强制真实下发
            socketClient?.Close();
            socketClient = null;
        }
        public async Task CloseToTriggerReconnectionAsync(CancellationToken token = default) {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] CloseToTriggerReconnectionAsync start, _currentPSet={_currentPSet}, hasRunTask={_runTaskTask != null}");
            _currentPSet = -1;
            socketClient?.Close();
            socketClient = null;
            _connectInProgress = 0;

            if (_runTaskTask != null) {
                var runTask = _runTaskTask;
                _runTaskTask = null;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try {
                    await runTask.WaitAsync(TimeSpan.FromSeconds(3), token);
                    sw.Stop();
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Old RunTask exited cleanly after {sw.ElapsedMilliseconds}ms");
                } catch (TimeoutException) {
                    sw.Stop();
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Old RunTask did not exit within 3s timeout (elapsed={sw.ElapsedMilliseconds}ms)");
                } catch (OperationCanceledException) {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] CloseToTriggerReconnectionAsync cancelled after {sw.ElapsedMilliseconds}ms");
                }
            } else {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] No RunTask to wait for, proceeding directly");
            }
        }
        // public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        public override bool WorkplaceCheckConnection() => Connected;
        #endregion

        #region Methods
        private async Task<bool> ConnectToServer() {
            try {
                if (Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Already connected");
                    return false;
                }

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connecting to {_ip}:{_port}");
                bool pingSuccess = false;
                bool connectSuccess = false;
                bool sendConnectMsgSuceess = false;
                bool dataEnableMsgSuccess = false;

                // 1. check ping
                pingSuccess = MainUtils.PingHost(_ip);
                if (pingSuccess) {
                    // 2. check socket
                    try {
                        socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socketClient.ReceiveTimeout = ReceiveTimeout;
                        socketClient.SendTimeout = 500;
                        socketClient.Connect(IPAddress.Parse(_ip), _port);
                        connectSuccess = true;
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Socket connected");

                        // 3. send connecting message
                        if (connectSuccess && _toolType is ToolPFSeries toolPF) {
                            if (toolPF.COMMAND_CONNECT_ASCII != null) {
                                SendMessageRecevingCount = 0;
                                string? result1 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_CONNECT_ASCII.GetMessage());
                                if (result1 != null) {
                                    string mid1 = toolPF.GetMid(result1);
                                    sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";
                                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connect response: {mid1}");
                                } else {
                                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] No connect response");
                                    sendConnectMsgSuceess = false;
                                }

                                // 4. send data receving enable message
                                if (sendConnectMsgSuceess) {
                                    SendMessageRecevingCount = 0;
                                    string? result2 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_DATA_ASCII.GetMessage());
                                    if (result2 != null) {
                                        string mid2 = toolPF.GetMid(result2);
                                        dataEnableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Data enable response: {mid2}");
                                    } else {
                                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] No data enable response");
                                        dataEnableMsgSuccess = false;
                                    }

                                    // 5. send curve data receving enable message
                                    if (dataEnableMsgSuccess) {
                                        SendMessageRecevingCount = 0;
                                        await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_CURVE_ASCII.GetMessage());
                                    }
                                }
                            } else {
                                sendConnectMsgSuceess = true;
                                dataEnableMsgSuccess = true;
                            }
                        } else if (connectSuccess && _toolType is ToolFITFTC6 toolFitFTC6) {
                            sendConnectMsgSuceess = true;

                            SendMessageRecevingCount = 0;
                            string? result = await SendAndReceiveOnlyForPreparingAsync(toolFitFTC6.COMMAND_DATA_SUBSCRIBE.GetMessage());
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Data subscribe msg sent, result: {result}");
                            dataEnableMsgSuccess = true;
                        } else {
                            sendConnectMsgSuceess = true;
                            dataEnableMsgSuccess = true;
                        }
                    } catch (Exception e) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Socket connection error", e);
                    }
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Ping failed");
                }
                bool isConnected = pingSuccess && connectSuccess && sendConnectMsgSuceess && dataEnableMsgSuccess;

                if (isConnected) {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection successful");
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Connection failed");
                    socketClient?.Close();
                    socketClient = null;
                }
                return isConnected;
            } catch (Exception e) {
                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Connection error", e);
            }

            return false;
        }
        private bool SendCommand(string command) {
            if (!Connected) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Command not sent - not connected");
                return false;
            }

            try {
                byte[] data;
                if (_toolType is ToolPFSeries) {
                    data = Encoding.ASCII.GetBytes(command);
                } else if (_toolType is ToolSudongX7 || _toolType is ToolFITFTC6) {
                    data = MainUtils.ToBytes(command);
                } else {
                    data = new byte[0];
                }

                int? num;
                lock (_sendLock) {
                    num = socketClient?.Send(data);
                }
                if (num.HasValue && num.Value > 0) {
                    HeartBeatCounter = 0;
                    return true;
                }

                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Command sending failed");
            } catch (Exception ex) {
                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Command sending error", ex);
            }

            return false;
        }
        private async Task<string?> SendAndReceiveOnlyForPreparingAsync(string command) {
            SendMessageRecevingCount++;

            if (Connected && SendMessageRecevingCount < SendMessageRecevingTimes) {
                try {
                    // Reset heart beat counter to prevent multiple response
                    HeartBeatCounter = 0;

                    byte[] data;
                    if (_toolType is ToolPFSeries) {
                        data = Encoding.ASCII.GetBytes(command);
                    } else if (_toolType is ToolSudongX7 || _toolType is ToolFITFTC6) {
                        data = MainUtils.ToBytes(command);
                    } else {
                        data = new byte[0];
                    }

                    // Send under lock for socket safety, ReceiveAsync outside lock (no timeout)
                    byte[] msgBytes = new byte[1024 * 1024];
                    lock (_sendLock) {
                        socketClient.Send(data);
                    }
                    int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                    logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake response received, len={msgLen}");
                    string result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                    if (_toolType is ToolPFSeries) {
                        result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                    } else if (_toolType is ToolFITFTC6) {
                        result = Encoding.GetEncoding("GBK").GetString(msgBytes.Take(msgLen).ToArray());
                    } else {
                        result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                    }
                    return result;
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Handshake send/receive error", e);
                    return null;
                }
            } else {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] SendAndReceive aborted - not connected");
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] SendAndReceive aborted - max retries reached");
                }
            }
            return null;
        }
        public async Task<bool> SendPSetAsync(int pSetNumber) {
            if (pSetNumber == -1) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet failed - pset can not set to -1");
                return false;
            }
            if (!Connected) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet failed - not connected");
                return false;
            }
            if (_sendingPSet != -1) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet skipped - busy (sending={_sendingPSet})");
                return false;
            }
            if (_currentPSet == pSetNumber) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet skipped - already set to {pSetNumber}");
                return true;
            }

            try {
                _sendingPSet = pSetNumber;
                _psetSentOk = false;

                if (Connected) {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Sending PSet {pSetNumber}");

                    string command = "";
                    if (_toolType is ToolPFSeries toolPF) {
                        command = toolPF.GetPSetCommand(pSetNumber);
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        command = toolX7.GetPSetCommand(pSetNumber);
                    } else if (_toolType is ToolFITFTC6 toolFitFTC6) {
                        command = toolFitFTC6.GetPSetCommand(pSetNumber);
                    } else {
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type");
                    }

                    if (string.IsNullOrEmpty(command)) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] PSet failed - no command generated");
                        return false;
                    }

                    bool sendResult = false;
                    try {
                        sendResult = SendCommand(command);
                    } catch (Exception e) {
                        logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] PSet send error", e);
                    }

                    bool isSuccess;
                    if (sendResult) {
                        int waitTimes = 0;
                        while (!_psetSentOk && waitTimes < PSetWaitTimesMax) {
                            waitTimes++;
                            await Task.Delay(PSetWaitTime);
                        }

                        if (!_psetSentOk) {
                            isSuccess = false;
                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet sending timeout after {waitTimes * PSetWaitTime}ms (waited={waitTimes}, psetSentOk=false → tool rejected or no response within window)");
                        } else {
                            isSuccess = true;
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] PSet success: {_currentPSet} -> {pSetNumber}");

                            _currentPSet = _sendingPSet;
                        }
                    } else {
                        isSuccess = false;
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet send failed");
                    }

                    return isSuccess;
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet aborted - disconnected");
                }
            } catch (Exception e) {
                logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] PSet error", e);
            } finally {
                _sendingPSet = -1;
                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] SendPSetAsync finally, _sendingPSet reset to -1");
            }

            return false;
        }

        /// <summary>
        /// 断开当前连接 → 重连 → 单次发送 PSet，作为一次原子重试
        /// </summary>
        public async Task<bool> ReconnectAndResendPset(int pSetNumber, CancellationToken token) {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ReconnectAndResendPset pSetNumber={pSetNumber} start");

            // 1. 阻止 TaskCheckingLoop 并发重连
            Status = CONNECTING;

            // 2. 关闭旧连接并等待 RunTask 彻底退出（保证 finally 已执行）
            try {
                await CloseToTriggerReconnectionAsync(token);
            } catch (OperationCanceledException) {
                Status = DISCONNECTED;
                return false;
            }

            // 3. 启动重连
            Connect();
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connect() returned, polling for connection...");

            // 5. 轮询等待重连完成（200ms × 50 = 10s）
            int pollCount = 0;
            int pollMax = 50;
            while (!Connected && pollCount < pollMax && !token.IsCancellationRequested) {
                pollCount++;
                try {
                    await Task.Delay(200, token);
                } catch (OperationCanceledException) {
                    Status = DISCONNECTED;
                    return false;
                }
            }

            if (!Connected) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] ReconnectAndResendPset reconnect timeout after {pollMax * 200}ms");
                Status = DISCONNECTED;  // 恢复状态，让 TaskCheckingLoop 接管
                return false;
            }

            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ReconnectAndResendPset reconnected after {pollCount * 200}ms");

            // 6. 在新连接上单次发送 PSet（_currentPSet 已在 CloseToTriggerReconnectionAsync 中重置）
            bool psetResult = await SendPSetAsync(pSetNumber);
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] ReconnectAndResendPset SendPSetAsync result={psetResult}, pSetNumber={pSetNumber}");
            if (!psetResult) {
                Status = DISCONNECTED;  // PSet 失败视为本次重试整体失败，恢复状态让 TaskCheckingLoop 接管
            }
            return psetResult;
        }

        public void SendLock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Lock failed - not connected");
                    return;
                }
                if (_pendingLockCommand == PendingLockCommand.Lock) return;
                if (_locked && _pendingLockCommand != PendingLockCommand.Unlock) return;

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Locking");
                _pendingLockCommand = PendingLockCommand.Lock;
            }
            PerformLock();
        }

        public void ForceSendLock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force lock failed - not connected");
                    return;
                }

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force locking");
                _pendingLockCommand = PendingLockCommand.None;
                UpdateInternalLockState(true);
            }
            PerformLock();
        }

        private void PerformLock() {
            if (_toolType is ToolPFSeries toolPF) {
                SendCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
            } else if (_toolType is ToolSudongX7 toolX7) {
                string cmd = toolX7.GetLockCommand();
                bool sentOk = SendCommand(cmd);
                Thread.Sleep(200);
                sentOk = SendCommand(cmd);

                if (sentOk) {
                    // 速动没有 解/锁枪 反馈，因此发完就自己设置
                    UpdateInternalLockState(true);
                }
            } else if (_toolType is ToolFITFTC6 toolFitFTC6) {
                _lockStatusSending = true;
                SendCommand(toolFitFTC6.COMMAND_LOCK_ASCII.GetMessage());
            } else {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type");
                return;
            }
        }

        public void SendUnlock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock failed - not connected");
                    return;
                }
                if (_pendingLockCommand == PendingLockCommand.Unlock) return;
                if (!_locked && _pendingLockCommand != PendingLockCommand.Lock) return;

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Unlocking");
                _pendingLockCommand = PendingLockCommand.Unlock;
            }
            PerformUnlock();
        }

        public void ForceSendUnlock() {
            lock (LockSyncObject) {
                if (!Connected) {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlock failed - not connected");
                    return;
                }

                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlocking");
                _pendingLockCommand = PendingLockCommand.None;
                UpdateInternalLockState(false);
            }
            PerformUnlock();
        }

        private void PerformUnlock() {
            if (_toolType is ToolPFSeries toolPF) {
                SendCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
            } else if (_toolType is ToolSudongX7 toolX7) {
                string cmd = toolX7.GetUnlockCommand();
                bool sentOk = SendCommand(cmd);
                Thread.Sleep(200);
                sentOk = SendCommand(cmd);

                if (sentOk) {
                    // 速动没有 解/锁枪 反馈，因此发完就自己设置
                    UpdateInternalLockState(false);
                }
            } else if (_toolType is ToolFITFTC6 toolFitFTC6) {
                _lockStatusSending = false;
                SendCommand(toolFitFTC6.COMMAND_UNLOCK_ASCII.GetMessage());
            } else {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type");
                return;
            }
        }

        public void SendBarcode(string barcode) {
            if (!Connected) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Barcode not sent - not connected");
                return;
            }
            if (_toolType is not ToolPFSeries toolPF) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Barcode not sent - tool type {_toolType?.Name} does not support barcode");
                return;
            }
            if (string.IsNullOrEmpty(barcode)) {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Barcode not sent - barcode is empty");
                return;
            }
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Sending barcode [{barcode}]");
            string command = toolPF.GetBarcodeCommand(barcode);
            SendCommand(command);
        }

        private void UpdateInternalLockState(bool newLockedState) {
            bool oldLocked = _locked;
            _locked = newLockedState;
            _pendingLockCommand = PendingLockCommand.None;

            if (oldLocked != _locked) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Lock state: {oldLocked} -> {_locked}");
            }
        }
        #endregion
    }
}
