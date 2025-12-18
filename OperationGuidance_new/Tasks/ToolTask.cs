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
        private volatile int CurrentPSet = -1;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeTool _toolType;
        private int HeartBeatCounter;
        private int LockCounter;
        private int UnLockCounter;
        private int LockWaitTimeCounter;
        private readonly SemaphoreSlim _commandLock = new(1, 1);
        private TaskCompletionSource<bool> _pSetResponseTcs;
        private CancellationTokenSource? _pSetCts;
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
                try {
                    while (Connected) {
                        // Check if it's time to send heart beating command
                        if (HeartBeatCounter >= HeartBeatDelay) {
                            // Only check hart beat interval if heart beat command is not null
                            if (_toolType is ToolPFSeries toolPF && toolPF.COMMAND_HEART_ASCII != null) {
                                // Send heart beat command to controller
                                SendRawCommand(toolPF.COMMAND_HEART_ASCII.GetMessage());
                                logger.Info($"Sending heart beating command to TOOL[{_device_name} - {_ip}: {_port}]...");
                            }
                            // Reset heart beat counter even no command has been sent
                            HeartBeatCounter = 0;
                        }

                        // Check any message is waiting for receving 
                        try {
                            lock (SyncObject) {
                                byte[] msgBytes = new byte[1024 * 1024];
                                int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                                if (msgLen > 0) {
                                    AnalyzeData(msgBytes.Take(msgLen).ToArray());
                                }
                            }
                        } catch (SocketException se) {
                            if (se.ErrorCode == (int) SocketError.TimedOut) {
                                HeartBeatCounter += ReceiveTimeout;
                                Console.WriteLine($"No data received... ");
                            } else {
                                throw;
                            }
                        }

                        // Check for lock wait time
                        if (LockWaitTimeCounter >= LockWaitTime) {
                            LockWaitTimeCounter = 0;
                            LockCounter = 0;
                            UnLockCounter = 0;
                        }

                        // Looping interval
                        await Task.Delay(LoopingInterval);
                        HeartBeatCounter += LoopingInterval;
                        LockWaitTimeCounter += LoopingInterval;
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while running task for connection<TOOL[{_device_name} - {_ip}: {_port}]>, e: {e}");
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

            void AnalyzeData(byte[] msgBytes) {
                try {
                    logger.Info($"Analyzing msgBytes = [{string.Join(", ", msgBytes)}]");

                    // Analyse result
                    if (_toolType is ToolPFSeries toolPF2) {
                        toolPF2.AnalyzeData(msgBytes, (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            if (heartIsBeating != null) {
                                if (!heartIsBeating.Value) {
                                    throw new Exception("Heart is not beating...");
                                } else {
                                    logger.Info("Heart beating....");
                                }
                            }
                            // 在非 UI 线程中直接操作 TCS（无需 Task.Run）
                            if (pSetSendingOk.HasValue) {
                                // 设置 PSet 应答结果
                                _pSetResponseTcs.TrySetResult(pSetSendingOk.Value);
                            }
                            if (locked != null) {
                                _locked = locked.Value;
                            }
                            if (dataReceived != null && dataReceived.Value) { }
                            if (curveReceived != null && curveReceived.Value) {
                                socketClient.Send(Encoding.ASCII.GetBytes(toolPF2.COMMAND_CURVE_ACK_ASCII.GetMessage()));
                            }
                        }, _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        toolX7.AnalyzeData(msgBytes, (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            // 在非 UI 线程中直接操作 TCS（无需 Task.Run）
                            if (pSetSendingOk.HasValue) {
                                // 设置 PSet 应答结果
                                _pSetResponseTcs.TrySetResult(pSetSendingOk.Value);
                            }
                            if (locked != null) {
                                _locked = locked.Value;
                            }
                            if (dataReceived != null && dataReceived.Value) { }
                            if (curveReceived != null && curveReceived.Value) { }
                        }, _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
                    }
                } catch (Exception e) {
                    logger.Warn($"Error while analyzing msgBytes, e = [{e}]");
                }
            }
        }

        public override void Connect() {
            lock (SyncObject) {
                Task.Run(async () => {
                    HeartBeatCounter = 0;
                    CloseConnectionManually = false;

                    while (!Connected) {
                        Status = CONNECTING;

                        if (await ConnectToServer()) {
                            RunTask();
                            Status = CONNECTED;

                            InitVariablesAfterConnected();
                            break;
                        }
                        await Task.Delay(AutoReconnectingTrialDelay);
                    }
                });
            }
        }
        public override Task ConnectAsync() => Task.Run(() => Connect());
        public override void CloseConnection() {
            logger.Info($"Close connection<TOOL[{_device_name} - {_ip}: {_port}]> manually...");

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
        /// <summary>
        /// Performs deep connection validation using Socket.Poll.
        /// This provides more accurate connection status than Socket.Connected property alone.
        /// </summary>
        /// <returns>true if connection is valid and active, false otherwise</returns>
        private bool ValidateConnection() {
            try {
                if (socketClient == null || !socketClient.Connected || CloseConnectionManually) {
                    return false;
                }

                // Use Socket.Poll for quick connection check
                // Poll with SelectRead: returns true if connection is closed, reset, terminated,
                // or pending data is available. If Available is 0 after Poll returns true,
                // it means the connection has been closed/reset.
                if (socketClient.Poll(100000, SelectMode.SelectRead)) {
                    // Check if there's data available to read
                    if (socketClient.Available == 0) {
                        // No data available but Poll returned true - connection is closed
                        logger.Warn($"ValidateConnection: Connection to TOOL[{_device_name} - {_ip}: {_port}] appears to be closed");
                        return false;
                    }
                    // There's data available - connection is still valid
                }

                return true;
            } catch (SocketException se) {
                logger.Warn($"ValidateConnection: SocketException while validating connection to TOOL[{_device_name} - {_ip}: {_port}], error code: {se.ErrorCode}");
                return false;
            } catch (ObjectDisposedException) {
                logger.Warn($"ValidateConnection: Socket already disposed for TOOL[{_device_name} - {_ip}: {_port}]");
                return false;
            } catch (Exception e) {
                logger.Warn($"ValidateConnection: Unexpected error while validating connection to TOOL[{_device_name} - {_ip}: {_port}], e: {e.Message}");
                return false;
            }
        }

        private async Task<bool> ConnectToServer() {
            try {
                if (Connected) {
                    logger.Warn($"Already connecting to TOOL[{_device_name} - {_ip}: {_port}], please don't connect repeatedly.");
                    return false;
                }

                logger.Info($"Connecting to TOOL[{_device_name} - {_ip}: {_port}]");
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

                        // 3. send connecting message
                        if (connectSuccess && _toolType is ToolPFSeries toolPF) {
                            if (toolPF.COMMAND_CONNECT_ASCII != null) {
                                SendMessageRecevingCount = 0;
                                string? result1 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_CONNECT_ASCII.GetMessage());
                                if (result1 != null) {
                                    string mid1 = toolPF.GetMid(result1);
                                    logger.Info($"Mid for connect command is {mid1}");
                                    sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";
                                }

                                // 4. send data receving enable message
                                if (sendConnectMsgSuceess) {
                                    SendMessageRecevingCount = 0;
                                    string? result2 = await SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_DATA_ASCII.GetMessage());
                                    if (result2 != null) {
                                        string mid2 = toolPF.GetMid(result2);
                                        logger.Info($"Mid for data message command is {mid2}");
                                        dataEnableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                                    }

                                    // 5. send curve data receving enable message
                                    if (dataEnableMsgSuccess) {
                                        // Don't need to check result, because if PF6000 doesn't have any license for curve data, then it will return 0004 which means it failed, it can not retrieve any curve data
                                        SendMessageRecevingCount = 0;
                                        SendAndReceiveOnlyForPreparingAsync(toolPF.COMMAND_CURVE_ASCII.GetMessage());
                                    }
                                }
                            }
                        } else {
                            sendConnectMsgSuceess = true;
                            dataEnableMsgSuccess = true;
                        }
                    } catch (Exception e) {
                        logger.Warn($"Connect error while connecting to TOOL[{_device_name} - {_ip}: {_port}], e: {e}");
                    }
                } else {
                    logger.Warn($"Failed to connect to TOOL[{_device_name} - {_ip}: {_port}]");
                }
                bool isConnected = pingSuccess && connectSuccess && sendConnectMsgSuceess && dataEnableMsgSuccess;
                if (isConnected) {
                    MainUtils.Info(logger, $"Successfully connect to TOOL[{_device_name} - {_ip}: {_port}]");
                } else {
                    if (socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip)) {
                        socketClient.Close();
                        socketClient = null;
                    }
                }

                return isConnected;
            } catch (Exception e) {
                logger.Warn($"Failed to connect to TOOL[{_device_name} - {_ip}: {_port}], e = {e}");
            }

            return false;
        }

        private void InitVariablesAfterConnected() {
            // 初始化当前 pset
            CurrentPSet = -1;

            // 初始状态保持枪是解锁的
            ForceSendUnlock();
        }

        private async Task<string?> SendAndReceiveOnlyForPreparingAsync(string command) {
            SendMessageRecevingCount++;
            if (Connected && SendMessageRecevingCount < SendMessageRecevingTimes) {
                try {
                    // Reset heart beat counter to prevent multiple response
                    HeartBeatCounter = 0;
                    // Send command to controller
                    logger.Info($"Sending command[{command}] to Tool[{_device_name} - {_ip}: {_port}]");
                    socketClient.Send(Encoding.ASCII.GetBytes(command));

                    // Receive data
                    byte[] msgBytes = new byte[1024 * 1024];
                    logger.Info($"Waiting for receving response for command[{command}] to Tool[{_device_name} - {_ip}: {_port}]");
                    int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                    string result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                    logger.Info($"Received response[{result}] for command[{command}] to Tool[{_device_name} - {_ip}: {_port}]");
                    return result;
                } catch (Exception e) {
                    logger.Error($"Error while sending command[{command}] to Tool[{_device_name} - {_ip}: {_port}], e: {e}");
                    return await SendAndReceiveOnlyForPreparingAsync(command);
                }
            }
            return null;
        }

        public async Task<bool> SendPSetAsync(int pSetNumber) {
            if (pSetNumber == CurrentPSet) {
                logger.Info($"PSet unchanged: {pSetNumber}");
                return true;
            }

            if (!ValidateConnection())
                return false;

            // 1. 获取锁（进入临界区）
            await _commandLock.WaitAsync();
            try {
                // 2. 创建新的 TCS（关键！）
                _pSetResponseTcs = new();

                string command = _toolType switch {
                    ToolPFSeries pf => pf.GetPSetCommand(pSetNumber),
                    ToolSudongX7 x7 => x7.GetPSetCommand(pSetNumber),
                    _ => throw new NotSupportedException($"Unsupported tool: {_toolType}")
                };

                if (string.IsNullOrEmpty(command))
                    return true;

                logger.Info($"Sending PSet {pSetNumber}: {command}");
                SendRawCommand(command);

                // 等待应答（5秒超时）
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                bool success = await WaitForPSetResponse(cts.Token);

                if (success)
                    CurrentPSet = pSetNumber;

                return success;
            } finally {
                // 4. 退出临界区
                _commandLock.Release();
            }
        }

        // 新增：纯发送（不入队、不重试）
        private void SendRawCommand(string command) {
            if (!Connected) return;

            byte[] data = _toolType is ToolSudongX7
                ? MainUtils.ToBytes(command)
                : Encoding.ASCII.GetBytes(command);

            socketClient?.Send(data);
        }

        // 替换原来的 PSetOk 字段（移除 volatile bool? PSetOk）
        // 改为在 AnalyzeData 回调中设置 TCS
        private async Task<bool> WaitForPSetResponse(CancellationToken cancellationToken) {
            _pSetCts?.Cancel();
            _pSetCts?.Dispose();
            _pSetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            using var _ = _pSetCts.Token.Register(() => _pSetResponseTcs.TrySetCanceled());

            try {
                logger.Debug("Waiting for PSet response...");
                bool result = await _pSetResponseTcs.Task;
                logger.Debug($"PSet response received: {result}");
                return result;
            } catch (OperationCanceledException) {
                logger.Warn("PSet response timeout");
                return false;
            }
        }

        public void ForceSendLock(bool ignoreLocalLockState = true) {
            if (!ValidateConnection()) {
                _locked = true;
                logger.Warn("ForceSendLock: Connection invalid, set _locked=true (safe state)");
                return;
            }

            if (!ignoreLocalLockState && _locked) {
                logger.Info("Already locked, skipping");
                return;
            }

            logger.Info($"ForceSendLock... (ignoreLocalLockState={ignoreLocalLockState})");

            if (_toolType is ToolPFSeries pf) {
                SendRawCommand(pf.COMMAND_LOCK_ASCII.GetMessage());
            } else if (_toolType is ToolSudongX7 x7) {
                SendRawCommand(x7.COMMAND_LOCK_ASCII.GetMessage());
                Thread.Sleep(500);
                SendRawCommand(x7.COMMAND_LOCK_ASCII.GetMessage());
            } else {
                logger.Warn($"ForceSendLock: Unsupported tool type [{_toolType?.GetType().Name ?? "Unknown"}]");
            }
        }

        public void ForceSendUnlock(bool ignoreLocalLockState = true) {
            if (!ValidateConnection()) {
                _locked = true;
                logger.Warn("ForceSendUnlock: Connection invalid, set _locked=true (safe state)");
                return;
            }

            if (!ignoreLocalLockState && !_locked) {
                logger.Info("Already unlocked, skipping");
                return;
            }

            logger.Info($"ForceSendUnlock... (ignoreLocalLockState={ignoreLocalLockState})");

            if (_toolType is ToolPFSeries pf) {
                SendRawCommand(pf.COMMAND_UNLOCK_ASCII.GetMessage());
            } else if (_toolType is ToolSudongX7 x7) {
                SendRawCommand(x7.COMMAND_UNLOCK_ASCII.GetMessage());
                Thread.Sleep(500);
                SendRawCommand(x7.COMMAND_UNLOCK_ASCII.GetMessage());
            } else {
                logger.Warn($"ForceSendUnlock: Unsupported tool type [{_toolType?.GetType().Name ?? "Unknown"}]");
            }
        }

        private void SendLock() {
            if (!ValidateConnection()) {
                _locked = true;
                logger.Info("Locking skipped - not connected");
                return;
            }

            if (LockCounter >= LockMaxTimes) {
                logger.Info("Lock max retry reached, skip");
                return;
            }

            logger.Info("Locking tool...");
            if (_toolType is ToolPFSeries toolPF) {
                SendRawCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
            } else if (_toolType is ToolSudongX7 toolX7) {
                if (!_locked) {
                    SendRawCommand(toolX7.COMMAND_LOCK_ASCII.GetMessage());
                    Thread.Sleep(500);
                    SendRawCommand(toolX7.COMMAND_LOCK_ASCII.GetMessage());
                }
            } else {
                logger.Warn($"SendLock: Unsupported tool type [{_toolType?.GetType().Name ?? "Unknown"}]");
            }

            LockCounter++;
        }

        private void SendUnlock() {
            if (!ValidateConnection()) {
                _locked = true;
                logger.Info("Unlocking skipped - not connected");
                return;
            }

            if (UnLockCounter >= UnLockMaxTimes) {
                logger.Info("Unlock max retry reached, skip");
                return;
            }

            logger.Info("Unlocking tool...");
            if (_toolType is ToolPFSeries toolPF) {
                SendRawCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
            } else if (_toolType is ToolSudongX7 toolX7) {
                if (_locked) {
                    SendRawCommand(toolX7.COMMAND_UNLOCK_ASCII.GetMessage());
                    Thread.Sleep(500);
                    SendRawCommand(toolX7.COMMAND_UNLOCK_ASCII.GetMessage());
                }
            } else {
                logger.Warn($"SendUnlock: Unsupported tool type [{_toolType?.GetType().Name ?? "Unknown"}]");
            }

            UnLockCounter++;
        }
        #endregion
    }
}
