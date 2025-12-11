using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Abstracts;
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
        private int? CurrentPSet = null;
        private bool? PSetOk = false;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeTool _toolType;
        private int HeartBeatCounter;
        private int LockCounter;
        private int UnLockCounter;
        private int LockWaitTimeCounter;
        private Queue<string> _commands = new();
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
        public Queue<string> Commands { get => _commands; set => _commands = value; }
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
        protected override async Task RunTaskAsync(CancellationToken cancellationToken = default) {
            try {
                while (!cancellationToken.IsCancellationRequested && Connected) {
                    // Check if it's time to send heart beating command
                    if (HeartBeatCounter >= HeartBeatDelay) {
                        // Only check hart beat interval if heart beat command is not null
                        if (_toolType is ToolPFSeries toolPF && toolPF.COMMAND_HEART_ASCII != null) {
                            // Send heart beat command to controller
                            SendCommand(toolPF.COMMAND_HEART_ASCII.GetMessage());
                            logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Sending heart beating command to {_device_name}"));
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
                    await Task.Delay(LoopingInterval, cancellationToken);
                    HeartBeatCounter += LoopingInterval;
                    LockWaitTimeCounter += LoopingInterval;
                }
            } catch (OperationCanceledException) {
                logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Task execution cancelled for {_device_name}"));
            } catch (Exception e) {
                logger.Warn(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Error while running task for {_device_name}: {e.Message}"));
            } finally {
                logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Disconnected from {_device_name}"));
                if (socketClient != null) {
                    socketClient.Close();
                    socketClient = null;
                }
                if (CloseConnectionManually) {
                    logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Socket connection closed manually for {_device_name}, won't reconnect"));
                }
            }

            void AnalyzeData(byte[] msgBytes) {
                try {
                    logger.Info($"Analyzing msgBytes = [{string.Join(", ", msgBytes)}]");

                    // Analyse result
                    if (_toolType is ToolPFSeries toolPF2) {
                        toolPF2.AnalyzeData(msgBytes, async (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            // Execute asynchronously without Task.Run
                            await Task.Yield(); // Yield to avoid blocking
                            if (heartIsBeating != null) {
                                if (!heartIsBeating.Value) {
                                    throw new Exception("Heart is not beating...");
                                } else {
                                    logger.Info("Heart beating....");
                                }
                            }
                            if (pSetSendingOk != null) {
                                PSetOk = pSetSendingOk.Value;
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
                        toolX7.AnalyzeData(msgBytes, async (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                            // Execute asynchronously without Task.Run
                            await Task.Yield(); // Yield to avoid blocking
                            if (pSetSendingOk != null) {
                                PSetOk = pSetSendingOk.Value;
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

        public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
            lock (SyncObject) {
                HeartBeatCounter = 0;
                CloseConnectionManually = false;
            }

            return await ConnectWithRetryAsync(async (ct) => {
                if (await ConnectToServer(ct)) {
                    // Start the task loop
                    _ = Task.Run(async () => {
                        await RunTaskAsync(cancellationToken);
                    }, cancellationToken);

                    // Send unlock command after successful connection
                    ForceSendUnlock();
                    return true;
                }
                return false;
            }, cancellationToken: cancellationToken);
        }

        public override async Task CloseConnectionAsync(CancellationToken cancellationToken = default) {
            await Task.Run(() => {
                logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Close connection manually for {_device_name}"));

                if (Connected) {
                    socketClient.Close();
                    socketClient = null;
                }

                CloseConnectionManually = true;
            }, cancellationToken);
        }

        // public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        [Obsolete("Use WorkplaceCheckConnectionAsync with CancellationToken instead")]
        public override bool WorkplaceCheckConnection() => Connected;
        public override async Task<bool> WorkplaceCheckConnectionAsync(CancellationToken cancellationToken = default) {
            return await Task.FromResult(Connected);
        }
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

        private async Task<bool> ConnectToServer(CancellationToken cancellationToken = default) {
            try {
                if (Connected) {
                    logger.Warn(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Already connecting for {_device_name}, please don't connect repeatedly"));
                    return false;
                }

                logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Connecting to {_device_name}"));
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
                        logger.Warn(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Connect error for {_device_name}: {e.Message}"));
                    }
                } else {
                    logger.Warn(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Failed to connect to {_device_name}"));
                }
                bool isConnected = pingSuccess && connectSuccess && sendConnectMsgSuceess && dataEnableMsgSuccess;
                if (isConnected) {
                    MainUtils.Info(logger, MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Successfully connect to {_device_name}"));
                } else {
                    if (socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip)) {
                        socketClient.Close();
                        socketClient = null;
                    }
                }
                return isConnected;
            } catch (Exception e) {
                logger.Warn(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Failed to connect to {_device_name}: {e.Message}"));
            }

            return false;
        }
        private void SendCommand(string command) {
            if (!_commands.Contains(command)) {
                // Enqueue to avoid duplicated calls
                _commands.Enqueue(command);
            }

            try {
                // Reset heart beat counter
                HeartBeatCounter = 0;

                // Send command
                if (_toolType is ToolPFSeries toolPF2) {
                    socketClient.Send(Encoding.ASCII.GetBytes(command));
                } else if (_toolType is ToolSudongX7 toolX7) {
                    socketClient.Send(MainUtils.ToBytes(command));
                }

                // Dequeue to allow new command to enqueue
                _commands.Dequeue();
            } catch (Exception ex) {
                logger.Error(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Error while sending command to {_device_name}: {ex.Message}"), ex);
            }
        }
        private async Task<string?> SendAndReceiveOnlyForPreparingAsync(string command) {
            SendMessageRecevingCount++;
            if (Connected && SendMessageRecevingCount < SendMessageRecevingTimes) {
                try {
                    // Reset heart beat counter to prevent multiple response
                    HeartBeatCounter = 0;
                    // Send command to controller
                    logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Sending command[{command}] to {_device_name}"));
                    socketClient.Send(Encoding.ASCII.GetBytes(command));

                    // Receive data
                    byte[] msgBytes = new byte[1024 * 1024];
                    logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Waiting for response for command[{command}] to {_device_name}"));
                    int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                    string result = Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                    logger.Info(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Received response[{result}] for command[{command}] to {_device_name}"));
                    return result;
                } catch (Exception e) {
                    logger.Error(MainUtils.FormatDeviceLog("TOOL", $"{_ip}:{_port}", $"Error while sending command[{command}] to {_device_name}: {e.Message}"), e);
                    return await SendAndReceiveOnlyForPreparingAsync(command);
                }
            }
            return null;
        }
        public async Task<bool> SendPSetAsync(int pSetNumber) {
            if (pSetNumber == CurrentPSet) {
                logger.Info($"Current pset is [{CurrentPSet}], same as sending one [{pSetNumber}], no need to send any command...");
                return true;
            }

            PSetOk = null;
            if (Connected) {
                return await Task.Run(async () => {
                    try {
                        logger.Info($"Setting pset to [{pSetNumber}]...");
                        string command = "";
                        if (_toolType is ToolPFSeries toolPF) {
                            command = toolPF.GetPSetCommand(pSetNumber);
                            logger.Info($"Sending command to {toolPF.Name}: {command}");
                        } else if (_toolType is ToolSudongX7 toolX7) {
                            command = toolX7.GetPSetCommand(pSetNumber);
                            logger.Info($"Sending command to {toolX7.Name}: {command}");
                        } else {
                        }

                        // Send pset
                        if (string.IsNullOrEmpty(command)) {
                            return true;
                        }
                        int waitTimesMax = 15;
                        int waitTimes = 0;
                        while (PSetOk == null && waitTimes < waitTimesMax) {
                            try {
                                SendCommand(command);
                                waitTimes++;
                            } catch (Exception e) {
                                logger.Error($"Error while sending command [{command}] (Setting pset to {pSetNumber})... Will retry for this...", e);
                            }

                            logger.Info("Waiting for pset ok .......");
                            await Task.Delay(PSetWaitTime);
                        }

                        if (PSetOk != null && PSetOk.Value) {
                            CurrentPSet = pSetNumber;
                        }

                        logger.Info($"Setting pset to [{pSetNumber}] [{PSetOk != null && PSetOk.Value}]!");
                    } catch (Exception e) {
                        logger.Error($"Error while setting pset to {pSetNumber}...", e);
                    }
                    return PSetOk != null && PSetOk.Value;
                });
            }
            return PSetOk != null && PSetOk.Value;
        }

        private void SendLock() {
            if (Connected) {
                if (LockCounter < LockMaxTimes) {
                    logger.Info($"Locking tool...");
                    if (_toolType is ToolPFSeries toolPF) {
                        SendCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        if (!_locked) {
                            SendCommand(toolX7.COMMAND_LOCK_ASCII.GetMessage());
                            Thread.Sleep(500);
                            SendCommand(toolX7.COMMAND_LOCK_ASCII.GetMessage());
                            _locked = true;
                        }
                    } else {
                        logger.Warn($"SendLock: Unsupported tool type [{_toolType?.GetType().Name ?? "Unknown"}] for TOOL[{_device_name} - {_ip}: {_port}]");
                    }

                    LockCounter++;
                }
            } else {
                _locked = false;
                logger.Info($"Locking failure, it's not connected...");
            }
        }
        public void ForceSendLock(bool ignoreLocalLockState = false) {
            // Use deep connection validation instead of simple Connected check
            if (!ValidateConnection()) {
                _locked = true;  // Set to safe state (locked) when connection is invalid
                logger.Warn($"ForceSendLock: Connection validation failed for TOOL[{_device_name} - {_ip}: {_port}], setting _locked to true (safe state)");
                return;
            }

            logger.Info($"Force locking tool... (ignoreLocalLockState={ignoreLocalLockState})");
            if (_toolType is ToolPFSeries toolPF) {
                if (ignoreLocalLockState || !_locked) {
                    SendCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
                    _locked = true;  // Force update local state
                    logger.Info($"Lock command sent, updated local _locked state to true");
                } else {
                    logger.Info($"Skip lock command - already locked (ignoreLocalLockState={ignoreLocalLockState}, _locked={_locked})");
                }
            } else if (_toolType is ToolSudongX7 toolX7) {
                SendCommand(toolX7.COMMAND_LOCK_ASCII.GetMessage());
                Thread.Sleep(500);
                SendCommand(toolX7.COMMAND_LOCK_ASCII.GetMessage());
                _locked = true;
                logger.Info($"Lock command sent for ToolSudongX7, updated local _locked state to true");
            } else {
                logger.Warn($"ForceSendLock: Unsupported tool type [{_toolType?.GetType().Name ?? "Unknown"}] for TOOL[{_device_name} - {_ip}: {_port}]");
            }
        }
        private void SendUnlock() {
            if (Connected) {
                if (UnLockCounter < UnLockMaxTimes) {
                    logger.Info($"Unlocking tool...");
                    if (_toolType is ToolPFSeries toolPF) {
                        SendCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        if (_locked) {
                            SendCommand(toolX7.COMMAND_UNLOCK_ASCII.GetMessage());
                            Thread.Sleep(500);
                            SendCommand(toolX7.COMMAND_UNLOCK_ASCII.GetMessage());
                            _locked = false;
                        }
                    } else {
                        logger.Warn($"SendUnlock: Unsupported tool type [{_toolType?.GetType().Name ?? "Unknown"}] for TOOL[{_device_name} - {_ip}: {_port}]");
                    }

                    UnLockCounter++;
                }
            } else {
                _locked = true;
                logger.Info($"Unlocking failure, it's not connected...");
            }
        }
        public void ForceSendUnlock(bool ignoreLocalLockState = false) {
            // Use deep connection validation instead of simple Connected check
            if (!ValidateConnection()) {
                _locked = true;  // Set to safe state (locked) when connection is invalid
                logger.Warn($"ForceSendUnlock: Connection validation failed for TOOL[{_device_name} - {_ip}: {_port}], setting _locked to true (safe state)");
                return;
            }

            logger.Info($"Force unlocking tool... (ignoreLocalLockState={ignoreLocalLockState})");
            if (_toolType is ToolPFSeries toolPF) {
                if (ignoreLocalLockState || _locked) {
                    SendCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
                    _locked = false;  // Force update local state
                    logger.Info($"Unlock command sent, updated local _locked state to false");
                } else {
                    logger.Info($"Skip unlock command - already unlocked (ignoreLocalLockState={ignoreLocalLockState}, _locked={_locked})");
                }
            } else if (_toolType is ToolSudongX7 toolX7) {
                SendCommand(toolX7.COMMAND_UNLOCK_ASCII.GetMessage());
                Thread.Sleep(500);
                SendCommand(toolX7.COMMAND_UNLOCK_ASCII.GetMessage());
                _locked = false;
                logger.Info($"Unlock command sent for ToolSudongX7, updated local _locked state to false");
            } else {
                logger.Warn($"ForceSendUnlock: Unsupported tool type [{_toolType?.GetType().Name ?? "Unknown"}] for TOOL[{_device_name} - {_ip}: {_port}]");
            }
        }
        #endregion
    }
}
