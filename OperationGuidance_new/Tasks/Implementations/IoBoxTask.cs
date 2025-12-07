using System.Net;
using System.Net.Sockets;
using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Abstracts;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Tasks {
    public class IoBoxTask: ATaskBase {
        private ILog logger = MainUtils.GetLogger(typeof(IoBoxTask));

        #region Fields
        private static readonly object SocketSyncRoot = new();
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
        protected override async Task RunTaskAsync(CancellationToken cancellationToken = default) {
            try {
                while (!cancellationToken.IsCancellationRequested && Connected) {
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
                            if (ArrangerType.ActionAfterIoSignalReceived != null) {
                                string readResult = "empty";
                                try {
                                    readResult = SendCommand(ArrangerType.DeviceType.COMMAND_READ.GetMessage());
                                    // logger.Debug($"[_ioBoxType.Name:{ArrangerType.DeviceType.Name}] result: readResult = {readResult}");

                                    // Analyze data
                                    ArrangerType.DeviceType.AnalyzeReadResultData(readResult, ArrangerType.ActionAfterIoSignalReceived);
                                } catch (Exception e) {
                                    logger.Warn($"Exception has been thrown while reading from _ioBoxType.Name:{ArrangerType.DeviceType.Name}], readResult = [{readResult}], e = {e}");
                                }
                            }
                        }

                        // Check setter selector
                        if (SetterSelectorType != null) {
                            string readResult = "empty";
                            try {
                                if (SetterSelectorType is IoBoxTypeSetterSelectorPlus selectorPlus) {
                                    readResult = SendCommand(SetterSelectorType.DeviceType.COMMAND_READ.GetMessage());

                                    // Analyze data
                                    ((IoBoxSetterSelectorPlus) selectorPlus.DeviceType).AnalyzeDataAndAction(readResult);

                                    // Write based on data and current position
                                    SendCommand(((IoBoxSetterSelectorPlus) selectorPlus.DeviceType).LoopingWriteCommand().GetMessage());
                                } else if (SetterSelectorType.RetrieveResult && SetterSelectorType.ActionAfterIoSignalReceived != null) {
                                    readResult = SendCommand(SetterSelectorType.DeviceType.COMMAND_READ.GetMessage());

                                    // Analyze data
                                    SetterSelectorType.DeviceType.AnalyzeData(readResult, SetterSelectorType.ActionAfterIoSignalReceived);
                                }
                            } catch (Exception e) {
                                logger.Warn($"Exception has been thrown while reading from _ioBoxType.Name:{SetterSelectorType.DeviceType.Name}], readResult = [{readResult}], e = {e}");
                            }
                        }

                        // Common delay
                        await Task.Delay(LoopingInterval, cancellationToken);
                    }
                } catch (OperationCanceledException) {
                    logger.Info($"Task execution cancelled for IOBOX[ {_ip}: {_port}]");
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
            }
        public override async Task<bool> ConnectAsync(CancellationToken cancellationToken = default) {
            return await ConnectWithRetryAsync(async (ct) => {
                if (await ConnectToServerAsync(ct)) {
                    // Start the task loop
                    _ = Task.Run(async () => {
                        await RunTaskAsync(cancellationToken);
                    }, cancellationToken);

                    Status = CONNECTED;
                    logger.Info($"[IOBOX] Connection established successfully - IP: {_ip}, Port: {_port}");
                    return true;
                }
                return false;
            }, cancellationToken: cancellationToken);
        }

        public override async Task CloseConnectionAsync(CancellationToken cancellationToken = default) {
            await Task.Run(() => {
                logger.Info($"Close connection<IOBOX[ {_ip}: {_port}]> manually...");
                if (Connected) {
                    socketClient.Close();
                }
                CloseConnectionManually = true;
            }, cancellationToken);
        }

        public override bool WorkplaceCheckConnection() => Connected && MainUtils.PingHost(_ip);
        public override async Task<bool> WorkplaceCheckConnectionAsync(CancellationToken cancellationToken = default) {
            return await Task.FromResult(Connected && MainUtils.PingHost(_ip));
        }
        #endregion

        #region Methods
        private async Task<bool> ConnectToServerAsync(CancellationToken cancellationToken = default) {
            if (Connected) {
                logger.Warn($"Already connecting to IOBOX[ {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            bool pingSuccess = false;
            bool connectSuccess = false;

            pingSuccess = MainUtils.PingHost(_ip);
            if (!pingSuccess) {
                return false;
            }

            if (pingSuccess) {
                try {
                    // Check for cancellation before creating socket
                    cancellationToken.ThrowIfCancellationRequested();

                    socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = ReceiveTimeout;

                    // Use async connect with timeout
                    var connectTask = socketClient.ConnectAsync(IPAddress.Parse(_ip), _port);
                    var timeoutTask = Task.Delay(5000, cancellationToken);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask) {
                        // Timeout occurred
                        socketClient.Close();
                        socketClient = null;
                        return false;
                    }

                    // Check if task was cancelled
                    cancellationToken.ThrowIfCancellationRequested();

                    connectSuccess = true;
                } catch (OperationCanceledException) {
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                    throw; // Re-throw to let ConnectWithRetryAsync handle it
                } catch (Exception e) {
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                }
            }
            return pingSuccess && connectSuccess;
        }

        // Backward compatibility wrapper (deprecated)
        private bool ConnectToServer(CancellationToken cancellationToken = default) {
            try {
                return ConnectToServerAsync(cancellationToken).GetAwaiter().GetResult();
            } catch (AggregateException ex) {
                throw ex.InnerException ?? ex;
            }
        }
        public string SendCommand(string command) {
            if (Connected) {
                try {
                    lock (SocketSyncRoot) {
                        // Send command to controller
                        socketClient.Send(MainUtils.ToBytes(command));

                        // Receive data
                        byte[] msgBytes = new byte[1024 * 1024];
                        int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                        return MainUtils.ToHexString(msgBytes.Take(msgLen).ToArray());
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

        internal string SendCommand(object value) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
