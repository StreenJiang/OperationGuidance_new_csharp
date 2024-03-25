using System.Net;
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
        private DeviceTypeCommunication _comminucationType;
        #endregion

        #region Properties
        // Override properties
        public override bool Connected => socketClient != null && socketClient.Connected && MainUtils.PingHost(_ip) && !CloseConnectionManually;
        // Other properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public DeviceTypeCommunication ComminucationType { get => _comminucationType; set => _comminucationType = value; }
        public Queue<string> Commands { get; set; }
        public string? Result { get; set; }
        public bool Locked { get; set; }
        #endregion

        #region Constructors
        public CommunicationTask(string? name, string ip, int port, DeviceTypeCommunication communication) {
            _device_name = name;
            _ip = ip;
            _port = port;
            _comminucationType = communication;
            DeviceType = communication;
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
                    System.Console.WriteLine($"Error while running task for connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]>: {e}");
                } finally {
                    System.Console.WriteLine($"Disconnected to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
                    if (socketClient != null) {
                        socketClient.Close();
                        socketClient = null;
                        Commands.Clear();
                    }
                    if (CloseConnectionManually) {
                        System.Console.WriteLine($"Socket connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]> has been closed manually, won't try to reconnecte anymore.");
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
            System.Console.WriteLine($"Close connection<COMMUNICATION[{_device_name} - {_ip}: {_port}]> manually...");
            if (Connected) {
                socketClient.Close();
            }
            CloseConnectionManually = true;
            Result = null;
            Commands.Clear();
        }
        #endregion

        #region Methods
        private bool ConnectToServer() {
            if (Connected) {
                System.Console.WriteLine($"Already connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}], please don't connect repeatedly.");
                return false;
            }

            System.Console.WriteLine($"Connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
            bool pingSuccess = false;
            bool connectSuccess = false;

            // 1. check ping
            pingSuccess = MainUtils.PingHost(_ip);
            if (pingSuccess) {
                // 2. check socket
                try {
                    socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = ReceiveTimeout;
                    socketClient.Connect(IPAddress.Parse(_ip), _port);
                    connectSuccess = true;
                    MainUtils.Log($"Successfully connect to COMMUNICATION[{_device_name} - {_ip}: {_port}]");
                } catch (Exception e) {
                    System.Console.WriteLine($"Error while connecting to COMMUNICATION[{_device_name} - {_ip}: {_port}]: {e}");
                }
            } else {
                System.Console.WriteLine($"Failed to ping COMMUNICATION[{_device_name} - {_ip}: {_port}]");
            }
            return pingSuccess && connectSuccess;
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
        #endregion
    }
}
