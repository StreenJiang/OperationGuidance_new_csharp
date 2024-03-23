using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Tasks {
    public class ToolTask: ATaskBase {
        #region Fields
        private readonly int ReceiveTimeout = 500;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeTool _tool;
        private int HeartBeatCounter = 0;
        private Action<TighteningData>? _actionAfterAnalysis;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && !CloseConnectionManually;
        // Other properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public Queue<string> Commands { get; set; }
        public string? Result { get; set; }
        public bool Locked { get; set; }
        public Action<TighteningData>? ActionAfterAnalysis { get => _actionAfterAnalysis; set => _actionAfterAnalysis = value; }
        #endregion

        #region Constructors
        public ToolTask(string? name, string ip, int port, DeviceTypeTool tool) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _tool = tool;
            Commands = new();
            Locked = false;
            Status = DISCONNECTED;
        }
        #endregion

        #region Override methods
        protected override void RunTask() {
            Task.Run(async () => {
                try {
                    while (Connected) {
                        // Check if any command
                        if (Commands.Count > 0) {
                            HeartBeatCounter = 0; // Reset heart beat counter to prevent multiple response
                            string command = Commands.Dequeue();
                            // Send command to controller
                            socketClient.Send(Encoding.ASCII.GetBytes(command));
                        } else {
                            if (_tool.COMMAND_HEART_ASCII != null &&HeartBeatCounter >= 5000) {
                                HeartBeatCounter = 0;
                                System.Console.WriteLine("Send heart beat command to keep alive...");
                                // Send heart beat command to controller
                                Commands.Enqueue(_tool.COMMAND_HEART_ASCII.GetMessage());
                            }
                        }
                        // Looping interval
                        await Task.Delay(LoopingInterval);
                        HeartBeatCounter += LoopingInterval;
                    }
                } catch (Exception e) {
                    System.Console.WriteLine($"Error: {e}");
                } finally {
                    System.Console.WriteLine($"Disconnected to TOOL[{_device_name} - {_ip}: {_port}]");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                    if (CloseConnectionManually) {
                        System.Console.WriteLine($"Socket connection<TOOL[{_device_name} - {_ip}: {_port}]> has been closed manually, won't try to reconnecte anymore.");
                    }
                }
            });
            Task.Run(async () => {
                try {
                    while (Connected) {
                        try {
                            Result = await CheckResultAsync();
                        } catch (Exception e) {
                            System.Console.WriteLine($"No data received... e: {e}");
                        }
                    }
                } finally {
                    System.Console.WriteLine($"Disconnected while waiting responses...");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                    }
                }
            });
        }
        public override Task Connect() {
            return Task.Run(async () => {
                while (!Connected && !CloseConnectionManually) {
                    Status = CONNECTING;
                    if (ConnectToServer()) {
                        RunTask();
                        Status = CONNECTED;
                        break;
                    }
                    await Task.Delay(AuotReconnectingTrialDelay);
                }
            });
        }
        public override void CloseConnection() {
            System.Console.WriteLine($"Close connection<TOOL[{_device_name} - {_ip}: {_port}]> manually...");
            if (Connected) {
                socketClient.Close();
            }
            CloseConnectionManually = true;
            Result = null;
            Commands.Clear();
        }
        #endregion

        #region Methods
        private async Task<string?> CheckResultAsync() {
            string? result = null;
            if (Connected) {
                byte[] msgBytes = new byte[1024 * 1024];
                int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                string resultTemp = Encoding.ASCII.GetString(msgBytes);
                result = _tool.AnalyzeData(resultTemp, _actionAfterAnalysis);
            }
            // 这个方法封装起来就是为了让 result awaitable
            return result;
        }
        private bool ConnectToServer() {
            if (Connected) {
                System.Console.WriteLine($"Already connecting to TOOL[{_device_name} - {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            System.Console.WriteLine($"Connecting to TOOL[{_device_name} - {_ip}: {_port}]");
            bool pingSuccess = false;
            bool connectSuccess = false;
            bool sendConnectMsgSuceess = false;
            bool enableMsgSuccess = false;

            // 1. check ping
            pingSuccess = PingHost(_ip);
            if (pingSuccess) {
                // 2. check socket
                try {
                    socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = ReceiveTimeout;
                    socketClient.Connect(IPAddress.Parse(_ip), _port);
                    connectSuccess = true;

                    // 3. send connecting message
                    if (connectSuccess && _tool.COMMAND_CONNECT_ASCII != null) {
                        string connectCommand = _tool.COMMAND_CONNECT_ASCII.GetMessage();
                        socketClient.Send(Encoding.ASCII.GetBytes(connectCommand));
                        byte[] msgBytes = new byte[1024 * 1024];
                        int msgLen = socketClient.Receive(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                        string response = Encoding.ASCII.GetString(msgBytes);
                        string mid1 = response.Substring(4, 4);
                        sendConnectMsgSuceess = mid1 == "0002" || mid1 == "0005";

                        // 4. send data receving enable message
                        if (sendConnectMsgSuceess && _tool.COMMAND_DATA_ASCII != null) {
                            string enableMsgCommand = _tool.COMMAND_DATA_ASCII.GetMessage();
                            socketClient.Send(Encoding.ASCII.GetBytes(enableMsgCommand));
                            byte[] msgBytes2 = new byte[1024 * 1024];
                            int msgLen2 = socketClient.Receive(new ArraySegment<byte>(msgBytes2), SocketFlags.None);
                            string response2 = Encoding.ASCII.GetString(msgBytes2);
                            string mid2 = response2.Substring(4, 4);
                            enableMsgSuccess = mid2 == "0002" || mid2 == "0005";
                            if (enableMsgSuccess) {
                                MainUtils.Log($"Successfully connect to TOOL[{_device_name} - {_ip}: {_port}]");
                                // Lock tool to keep safe
                                SendLock();
                            }
                        }
                    }
                } catch (Exception e) {
                    System.Console.WriteLine($"Connect error: {e}");
                }
            } else {
                System.Console.WriteLine($"Failed to connect to TOOL[{_device_name} - {_ip}: {_port}]");
            }
            return pingSuccess && connectSuccess && sendConnectMsgSuceess && enableMsgSuccess;
        }
        private bool PingHost(string namrOrAddress) {
            Ping? pinger = null;
            try {
                pinger = new();
                PingReply pingReply = pinger.Send(namrOrAddress);
                return pingReply.Status == IPStatus.Success;
            } catch (PingException pe) {
                System.Console.WriteLine($"Ping error: {pe}");
                return false;
            } finally {
                if (pinger != null) {
                    pinger.Dispose();
                }
            }
        }
        public void SendCommand(string command) {
            Commands.Enqueue(command);
        }
        public async Task<string?> SendCommandAsync(string command) {
            Result = null;
            SendCommand(command);

            string? result = null;
            int trialTime = 0;
            while (Connected && result == null) {
                if (trialTime > ReceiveTimeout) {
                    System.Console.WriteLine("Can't get any response at all, probably some other connection robbed it...");
                    break;
                }
                result = Result;
                if (result == null) {
                    await Task.Delay(LoopingInterval);
                    trialTime += LoopingInterval;
                }
            }
            Result = null;
            return result;
        }
        public void SendPSet(int pSetNumber) {
            if (pSetNumber <= 0 || pSetNumber >= 999) {
                WidgetUtils.ShowErrorPopUp("程序号范围必须在 0 ~ 999 以内！");
            } else if (_tool.COMMAND_PSET_ASCII != null) {
                string command = _tool.COMMAND_PSET_ASCII.GetMessage(pSetNumber.ToString("000"));
                System.Console.WriteLine($"Send pset command: {command}");
                SendCommand(command);
            }
        }
        public async Task<bool> SendPSetAsync(int pSetNumber) {
            if (pSetNumber <= 0 || pSetNumber >= 999) {
                WidgetUtils.ShowErrorPopUp("程序号范围必须在 0 ~ 999 以内！");
            } else if (_tool.COMMAND_PSET_ASCII != null) {
                string command = _tool.COMMAND_PSET_ASCII.GetMessage(pSetNumber.ToString("000"));
                System.Console.WriteLine($"Send pset command: {command}");
                string? result = await SendCommandAsync(command);
                System.Console.WriteLine($"Send pset command - result: {result}");
                return result != null && _tool.GetMidFromResult(result) == "0005";
            }
            return false;
        }
        public async void SendLock() {
            if (_tool.COMMAND_LOCK_ASCII != null && !Locked) {
                string command = _tool.COMMAND_LOCK_ASCII.GetMessage();
                System.Console.WriteLine($"Send lock command: {command}");
                string? result = await SendCommandAsync(command);
                if (result != null && _tool.GetMidFromResult(result) == "0005") {
                    Locked = true;
                }
            }
        }
        public async Task<bool> SendLockAsync() {
            if (_tool.COMMAND_LOCK_ASCII != null && !Locked) {
                string command = _tool.COMMAND_LOCK_ASCII.GetMessage();
                System.Console.WriteLine($"Send lock command: {command}");
                string? result = await SendCommandAsync(command);
                System.Console.WriteLine($"Send lock command - result: {result}");
                if (result != null && _tool.GetMidFromResult(result) == "0005") {
                    Locked = true;
                    return true;
                }
                return false;
            }
            return false;
        }
        public async void SendUnlock() {
            if (_tool.COMMAND_UNLOCK_ASCII != null && Locked) {
                string command = _tool.COMMAND_UNLOCK_ASCII.GetMessage();
                System.Console.WriteLine($"Send unlock command: {command}");
                string? result = await SendCommandAsync(command);
                if (result != null && _tool.GetMidFromResult(result) == "0005") {
                    Locked = false;
                }
            }
        }
        public async Task<bool> SendUnlockAsync() {
            if (_tool.COMMAND_UNLOCK_ASCII != null && Locked) {
                string command = _tool.COMMAND_UNLOCK_ASCII.GetMessage();
                System.Console.WriteLine($"Send unlock command: {command}");
                string? result = await SendCommandAsync(command);
                System.Console.WriteLine($"Send unlock command - result: {result}");
                if (result != null && _tool.GetMidFromResult(result) == "0005") {
                    Locked = false;
                    return true;
                }
                return false;
            }
            return false;
        }
        #endregion
    }
}
