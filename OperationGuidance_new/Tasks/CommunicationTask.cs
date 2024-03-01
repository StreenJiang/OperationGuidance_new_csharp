using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Tasks {
    public class CommunicationTask: ATaskBase {
        #region Fields
        private readonly int ReceiveTimeout = 500;
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private DeviceTypeCommunication _comminucation;
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
        #endregion

        #region Constructors
        public CommunicationTask(string? name, string ip, int port, DeviceTypeCommunication communication) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _comminucation = communication;
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
                            string command = Commands.Dequeue();
                            // Send command to controller
                            await socketClient.SendAsync(HexStrToBytes(command), SocketFlags.None);
                            // Check response
                            try {
                                byte[] msgBytes = new byte[1024 * 1024];
                                int msgLen = await socketClient.ReceiveAsync(new ArraySegment<byte>(msgBytes), SocketFlags.None);
                                if (msgLen >= 5) {
                                    Result = BytesToHexStr(msgBytes.Take(msgLen).ToArray());
                                } else {
                                    Result = "";
                                }
                            } catch (Exception e) {
                                System.Console.WriteLine($"No data received...");
                            }
                        }
                    }
                } catch (Exception e) {
                    MainUtils.Log($"Error while running task for connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]>: {e}");
                } finally {
                    MainUtils.Log($"Disconnected to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                        Commands.Clear();
                    }
                    if (CloseConnectionManually) {
                        MainUtils.Log($"Socket connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]> has been closed manually, won't try to reconnecte anymore.");
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
            MainUtils.Log($"Close connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]> manually...");
            CloseConnectionManually = true;
            if (Connected) {
                socketClient.Close();
                Result = null;
                Commands.Clear();
            }
        }
        #endregion

        #region Methods
        private bool ConnectToServer() {
            if (Connected) {
                MainUtils.Log($"Already connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            MainUtils.Log($"Connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
            bool pingSuccess = false;
            bool connectSuccess = false;

            // 1. check ping
            pingSuccess = PingHost(_ip);
            if (pingSuccess) {
                // 2. check socket
                try {
                    socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = ReceiveTimeout;
                    socketClient.Connect(IPAddress.Parse(_ip), _port);
                    connectSuccess = true;
                    MainUtils.Log($"Connected to COMMUNICATION[{_device_name} - {_ip}: {_port}] successfully");
                } catch (Exception e) {
                    MainUtils.Log($"Error while connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}]: {e}");
                }
            } else {
                MainUtils.Log($"Failed to ping COMMUNICATION[{_device_name} - {_ip}: {_port}]");
            }
            return pingSuccess && connectSuccess;
        }
        private bool PingHost(string namrOrAddress) {
            Ping? pinger = null;
            try {
                pinger = new();
                PingReply pingReply = pinger.Send(namrOrAddress);
                return pingReply.Status == IPStatus.Success;
            } catch (PingException pe) {
                MainUtils.Log($"Ping error: {pe}");
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
                    MainUtils.Log("Can't get any response at all, probably some other connection robbed it...");
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
        #endregion
    }
}
