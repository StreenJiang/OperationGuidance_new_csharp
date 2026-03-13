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
        private readonly int PSetWaitTime = 200;
        private readonly int PSetWaitTimesMax = 5;
        private readonly int LockWaitTime = 5000;
        private int SendMessageRecevingCount = 0;
        private volatile bool _locked = false;
        private readonly object _pSetLock = new object();
        private volatile int _sendingPSet = -1;
        private volatile int _currentPSet = -1;
        private volatile bool _psetSentOk = false;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeTool _toolType;
        private int HeartBeatCounter;
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
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Task thread started");
                try {
                    while (Connected) {
                        // Check if it's time to send heart beating command
                        if (HeartBeatCounter >= HeartBeatDelay) {
                            // Only check hart beat interval if heart beat command is not null
                            if (_toolType is ToolPFSeries toolPF && toolPF.COMMAND_HEART_ASCII != null) {
                                // Send heart beat command to controller
                                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Sending heartbeat command");
                                SendCommand(toolPF.COMMAND_HEART_ASCII.GetMessage());
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
                        LockWaitTimeCounter += LoopingInterval;

                        if (LockWaitTimeCounter >= LockWaitTime) {
                            LockWaitTimeCounter = LockWaitTime;
                        }
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
                            if (locked != null) {
                                bool oldLocked = _locked;
                                _locked = locked.Value;
                                LockWaitTimeCounter = 0;
                                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Lock state: {oldLocked} -> {_locked}");
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
                            if (locked != null) {
                                bool oldLocked = _locked;
                                _locked = locked.Value;
                                LockWaitTimeCounter = 0;
                                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Lock state: {oldLocked} -> {_locked}");
                            }
                            if (dataReceived != null && dataReceived.Value) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Data received");
                            }
                            if (curveReceived != null && curveReceived.Value) {
                                logger.Debug($"[TOOL:{_device_name}-{_ip}:{_port}] Curve data received");
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
            lock (SyncObject) {
                Task.Run(async () => {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Initiating connection");
                    HeartBeatCounter = 0;
                    CloseConnectionManually = false;

                    int retryCount = 0;
                    while (!Connected) {
                        retryCount++;
                        Status = CONNECTING;

                        if (await ConnectToServer()) {
                            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Connection established");
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
                });
            }
        }
        public override Task ConnectAsync() => Task.Run(() => Connect());
        public override void CloseConnection() {
            logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Closing connection (manual)");

            if (Connected) {
                socketClient.Close();
                socketClient = null;
            }

            CloseConnectionManually = true;
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
                    if (socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip)) {
                        socketClient.Close();
                        socketClient = null;
                    }
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
                byte[] data = _toolType is ToolSudongX7
                                        ? MainUtils.ToBytes(command)
                                        : Encoding.ASCII.GetBytes(command);

                int? num;
                lock (SyncObject) {
                    num = socketClient?.Send(data);
                }
                if (num.HasValue && num.Value > 0) {
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

                    // Send command to controller
                    socketClient.Send(Encoding.ASCII.GetBytes(command));

                    // Receive data
                    byte[] msgBytes = new byte[1024 * 1024];
                    int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                    string result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                    return result;
                } catch (Exception e) {
                    logger.Error($"[TOOL:{_device_name}-{_ip}:{_port}] Send/receive error", e);
                    return await SendAndReceiveOnlyForPreparingAsync(command);
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
                            logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] PSet sending timeout");
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
            }

            return false;
        }

        public void SendLock() {
            if (Connected) {
                if (!_locked && LockWaitTimeCounter >= LockWaitTime) {
                    if (_toolType is ToolPFSeries toolPF) {
                        SendCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        string cmd = toolX7.GetLockCommand();
                        SendCommand(cmd);
                        Thread.Sleep(200);
                        SendCommand(cmd);
                        _locked = true;
                        LockWaitTimeCounter = 0;
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Locked");
                    } else {
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type");
                    }
                } else {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock skiped - _locked={_locked}, LockWaitTimeCounter={LockWaitTimeCounter}");
                }
            } else {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Lock failed - not connected");
            }
        }
        public void ForceSendLock() {
            if (Connected) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force locking");
                if (_toolType is ToolPFSeries toolPF) {
                    SendCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
                } else if (_toolType is ToolSudongX7 toolX7) {
                    string cmd = toolX7.GetLockCommand();
                    SendCommand(cmd);
                    Thread.Sleep(200);
                    SendCommand(cmd);
                    _locked = true;
                    LockWaitTimeCounter = 0;
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Locked");
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type");
                }
            } else {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force lock failed - not connected");
            }
        }
        public void SendUnlock() {
            if (Connected) {
                if (_locked && LockWaitTimeCounter >= LockWaitTime) {
                    if (_toolType is ToolPFSeries toolPF) {
                        SendCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        string cmd = toolX7.GetUnlockCommand();
                        SendCommand(cmd);
                        Thread.Sleep(200);
                        SendCommand(cmd);
                        _locked = false;
                        LockWaitTimeCounter = 0;
                        logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Unlocked");
                    } else {
                        logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type");
                    }
                } else {
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock skiped - _locked={_locked}, LockWaitTimeCounter={LockWaitTimeCounter}");
                }
            } else {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unlock failed - not connected");
            }
        }
        public void ForceSendUnlock() {
            if (Connected) {
                logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlocking");
                if (_toolType is ToolPFSeries toolPF) {
                    SendCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
                } else if (_toolType is ToolSudongX7 toolX7) {
                    string cmd = toolX7.GetUnlockCommand();
                    SendCommand(cmd);
                    Thread.Sleep(200);
                    SendCommand(cmd);
                    _locked = false;
                    LockWaitTimeCounter = 0;
                    logger.Info($"[TOOL:{_device_name}-{_ip}:{_port}] Unlocked");
                } else {
                    logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Unknown tool type");
                }
            } else {
                logger.Warn($"[TOOL:{_device_name}-{_ip}:{_port}] Force unlock failed - not connected");
            }
        }
        #endregion
    }
}
