using System.Net;
using System.Net.Sockets;
using System.Text;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AsbtractClasses;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Tasks {
    public class ToolTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(ToolTask));

        #region Fields
        private static readonly object SyncObject = new();
        private readonly int ReceiveTimeout = 200;
        private readonly int HeartBeatDelay = 10000;
        private readonly int PSetWaitTime = 500;
        private readonly int WaitCurveTimes = 2;
        private int WaitCurveTimesCount = 0;
        private int WaitTimes = 5;
        private int WaitTimesCount = 0;
        private bool Locked = false;
        private bool? PSetOk = false;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeTool _toolType;
        private int HeartBeatCounter;
        private Queue<string> _commands = new();
        private Action<TighteningData, int>? _actionAfterAnalysis;
        private Action<CurveDataTemp, int>? _actionAfterCurveDataReceived;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually;
        // Other properties
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
        protected override void RunTask() {
            Task.Run(async () => {
                try {
                    while (Connected) {
                        // Check if any command in queue
                        if (WaitCurveTimesCount <= 0) {
                            if (_commands.Count > 0) {
                                // Reset heart beat counter
                                HeartBeatCounter = 0;

                                // Send command
                                string command = _commands.Dequeue();
                                if (_toolType is ToolPFSeries toolPF2) {
                                    socketClient.Send(Encoding.ASCII.GetBytes(command));
                                } else if (_toolType is ToolSudongX7 toolX7) {
                                    socketClient.Send(MainUtils.ToBytes(command));
                                }

                                // Wait for a little bit
                                await Task.Delay(ReceiveTimeout);
                                HeartBeatCounter += ReceiveTimeout;
                            }
                            // Check if it's time to send heart beating command
                            else if (HeartBeatCounter >= HeartBeatDelay) {
                                // Only check hart beat interval if heart beat command is not null
                                if (_toolType is ToolPFSeries toolPF && toolPF.COMMAND_HEART_ASCII != null) {
                                    // Send heart beat command to controller
                                    _commands.Enqueue(toolPF.COMMAND_HEART_ASCII.GetMessage());
                                    logger.Info($"Sending heart beating command to TOOL[{_device_name} - {_ip}: {_port}]...");
                                } else {
                                    // Reset heart beat counter even no command has been sent
                                    HeartBeatCounter = 0;
                                }
                            }
                        } else {
                            if (WaitTimesCount > WaitTimes) {
                                WaitCurveTimesCount--;
                            }
                        }

                        // Check any message is waiting for receving 
                        try {
                            lock (SyncObject) {
                                byte[] msgBytes = new byte[1024 * 1024];
                                int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes));

                                // Analyse result
                                if (_toolType is ToolPFSeries toolPF2) {
                                    toolPF2.AnalyzeData(msgBytes.Take(msgLen).ToArray(), (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                                        if (heartIsBeating != null && !heartIsBeating.Value) {
                                            throw new Exception("Heart is not beating...");
                                        }
                                        if (pSetSendingOk != null) {
                                            PSetOk = pSetSendingOk.Value;
                                        }
                                        if (locked != null) {
                                            Locked = locked.Value;
                                        }
                                        if (dataReceived != null && dataReceived.Value) {
                                            socketClient.Send(Encoding.ASCII.GetBytes(toolPF2.COMMAND_DATA_ACK_ASCII.GetMessage()));
                                            WaitCurveTimesCount = WaitCurveTimes;
                                        }
                                        if (curveReceived != null && curveReceived.Value) {
                                            socketClient.Send(Encoding.ASCII.GetBytes(toolPF2.COMMAND_CURVE_ACK_ASCII.GetMessage()));
                                            WaitCurveTimesCount--;
                                        }
                                    }, _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
                                } else if (_toolType is ToolSudongX7 toolX7) {
                                    toolX7.AnalyzeData(msgBytes.Take(msgLen).ToArray(), (heartIsBeating, pSetSendingOk, locked, dataReceived, curveReceived) => {
                                        if (pSetSendingOk != null) {
                                            PSetOk = pSetSendingOk.Value;
                                        }
                                        if (locked != null) {
                                            Locked = locked.Value;
                                        }
                                        if (dataReceived != null && dataReceived.Value) {
                                            // WaitCurveTimesCount = WaitCurveTimes;
                                        }
                                        if (curveReceived != null && curveReceived.Value) {
                                            WaitCurveTimesCount--;
                                        }
                                    }, _actionAfterAnalysis, _actionAfterCurveDataReceived, DeviceId);
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
        }

        public override async void Connect() {
            HeartBeatCounter = 0;
            CloseConnectionManually = false;
            while (!Connected) {
                Status = CONNECTING;
                if (ConnectToServer()) {
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
            }
            CloseConnectionManually = true;
        }
        // public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        public override bool WorkplaceCheckConnection() => Connected;
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
            bool dataEnableMsgSuccess = false;
            bool curveEnableMsgSuccess = false;

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
                            string? result1 = SendAndReceiveOnlyForPreparing(toolPF.COMMAND_CONNECT_ASCII.GetMessage());
                            if (result1 != null) {
                                string mid1 = toolPF.GetMid(result1);
                                sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";
                            }

                            // 4. send data receving enable message
                            if (sendConnectMsgSuceess) {
                                string? result2 = SendAndReceiveOnlyForPreparing(toolPF.COMMAND_DATA_ASCII.GetMessage());
                                if (result2 != null) {
                                    string mid2 = toolPF.GetMid(result2);
                                    dataEnableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                                }

                                // 5. send curve data receving enable message
                                if (dataEnableMsgSuccess) {
                                    string? result3 = SendAndReceiveOnlyForPreparing(toolPF.COMMAND_CURVE_ASCII.GetMessage());
                                    if (result3 != null) {
                                        string mid3 = toolPF.GetMid(result3);
                                        curveEnableMsgSuccess = mid3 == "0005";
                                        MainUtils.Info(logger, $"Successfully connect to TOOL[{_device_name} - {_ip}: {_port}]");
                                    }
                                }
                            }
                        }
                    } else {
                        sendConnectMsgSuceess = true;
                        dataEnableMsgSuccess = true;
                        curveEnableMsgSuccess = true;
                    }
                } catch (Exception e) {
                    logger.Warn($"Connect error while connecting to TOOL[{_device_name} - {_ip}: {_port}], e: {e}");
                }
            } else {
                logger.Warn($"Failed to connect to TOOL[{_device_name} - {_ip}: {_port}]");
            }
            if (!(pingSuccess && connectSuccess && sendConnectMsgSuceess && dataEnableMsgSuccess && curveEnableMsgSuccess)) {
                if (socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip)) {
                    socketClient.Close();
                }
            }
            return pingSuccess && connectSuccess && sendConnectMsgSuceess && dataEnableMsgSuccess && curveEnableMsgSuccess;
        }
        private void SendCommand(string command) {
            if (!_commands.Contains(command)) {
                _commands.Enqueue(command);
            }
        }
        private string? SendAndReceiveOnlyForPreparing(string command) {
            if (Connected) {
                try {
                    // Reset heart beat counter to prevent multiple response
                    HeartBeatCounter = 0;
                    // Send command to controller
                    socketClient.Send(Encoding.ASCII.GetBytes(command));

                    // Receive data
                    byte[] msgBytes = new byte[1024 * 1024];
                    int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes));
                    return Encoding.ASCII.GetString(msgBytes.Take(msgLen).ToArray());
                } catch (Exception e) {
                    logger.Error($"Error while sending command[{command}] to Tool[{_device_name} - {_ip}: {_port}], e: {e}");
                    // throw e;
                }
            }
            return null;
        }
        public async Task<bool> SendPSetAsync(int pSetNumber) {
            PSetOk = null;
            if (Connected) {
                await Task.Run(async () => {
                    // _commands.Enqueue(_toolType.COMMAND_PSET_ASCII.GetMessage(pSetNumber.ToString("000")));
                    if (_toolType is ToolPFSeries toolPF) {
                        SendCommand(toolPF.GetPSetCommand(pSetNumber));
                    } else if (_toolType is ToolSudongX7 toolX7) {
                        SendCommand(toolX7.GetPSetCommand(pSetNumber));
                    } else {
                    }

                    int waitTimesMax = 15;
                    int waitTimes = 0;
                    while (PSetOk == null && waitTimes < waitTimesMax) {
                        waitTimes++;
                        await Task.Delay(PSetWaitTime);
                    }
                    return PSetOk != null && PSetOk.Value;
                });
            }
            return PSetOk != null && PSetOk.Value;
        }

        public void SendLock() {
            if (Connected && !Locked) {
                if (_toolType is ToolPFSeries toolPF) {
                    SendCommand(toolPF.COMMAND_LOCK_ASCII.GetMessage());
                } else if (_toolType is ToolSudongX7 toolX7) {
                    SendCommand(toolX7.COMMAND_LOCK_ASCII.GetMessage());
                    Locked = true;
                } else {
                }
            }
        }
        public void SendUnlock() {
            if (Connected && Locked) {
                if (_toolType is ToolPFSeries toolPF) {
                    SendCommand(toolPF.COMMAND_UNLOCK_ASCII.GetMessage());
                } else if (_toolType is ToolSudongX7 toolX7) {
                    SendCommand(toolX7.COMMAND_UNLOCK_ASCII.GetMessage());
                    Locked = false;
                } else {
                }
            }
        }
        public bool IsLocked() => Locked;
        #endregion
    }
}
