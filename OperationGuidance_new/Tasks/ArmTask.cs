using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Tasks {
    public class ArmTask {
        #region Fields
        private readonly int LoopingInterval = 25;
        private readonly int AuotReconnectingTrialDelay = 1000; // 断线重连尝试间隔
        private Socket? socketClient = null;
        private string _ip;
        private int _port;
        private string[] _coordinateCommands;
        private string _currentCommand;
        private Coordinates3D? _currentCoordinates;
        private Action<Coordinates3D> _actionAfterReceiving;
        #endregion

        #region Properties
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }
        public bool CloseConnectionManually { get; set; } = false;
        public Queue<string> Commands { get; set; } = new();
        public string? Result { get; set; }
        public bool RetrieveResult { get; set; } = false;
        public bool Connected => socketClient != null && socketClient.Connected;
        public event Action<Coordinates3D> ActionAfterReceiving { add => _actionAfterReceiving += value; remove => _actionAfterReceiving -= value; }
        #endregion

        public ArmTask(string ip, int port, string[] commands) {
            _ip = ip;
            _port = port;
            _coordinateCommands = commands;
            _actionAfterReceiving += c => {};
        }

        private void RunTask() {
            Task.Run(async () => {
                try {
                    while (Connected && !CloseConnectionManually) {
                        // Check if any command
                        if (Commands.Count > 0) {
                            _currentCommand = Commands.Dequeue();
                            // Send command to controller
                            await socketClient.SendAsync(HexStrToBytes(_currentCommand), SocketFlags.None);
                            // Check response
                            CheckResponse();
                        }
                    }
                } catch (Exception e) {
                    System.Console.WriteLine($"Error while running task for connection<{_ip}-{_port}>: {e}");
                } finally {
                    System.Console.WriteLine($"Disconnected to {_ip}-{_port}");
                    if (socketClient != null) {
                        if (!CloseConnectionManually) {
                            while (!ConnectToController()) {
                                await Task.Delay(AuotReconnectingTrialDelay);
                                System.Console.WriteLine($"Trying to reconnect to {_ip}-{_port}...");
                            }
                            RunTask();
                        } else {
                            socketClient.Close();
                            socketClient = null;
                        }
                    }
                }
            });
        }

        private async void CheckResponse() {
            if (Connected && !CloseConnectionManually) {
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

        private bool ConnectToController() {
            bool pingSuccess = false;
            bool connectSuccess = false;

            if (Connected) {
                System.Console.WriteLine($"Already connecting to {_ip}-{_port}, please don't connect repeatedly.");
                return false;
            }
            System.Console.WriteLine($"Connecting to {_ip}-{_port}");
            // 1. check ping
            pingSuccess = PingHost(_ip);
            if (pingSuccess) {
                // 2. check socket
                try {
                    socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = 500;
                    socketClient.Connect(IPAddress.Parse(_ip), _port);
                    connectSuccess = true;
                    System.Console.WriteLine($"Connected to {_ip}-{_port} successfully");
                } catch (Exception e) {
                    System.Console.WriteLine($"Error while connecting to {_ip}-{_port}: {e}");
                }
            } else {
                System.Console.WriteLine($"Failed to connect to {_ip}-{_port}");
            }
            return pingSuccess && connectSuccess;
        }

        private async Task<bool> ConnectToControllerAsync() {
            bool pingSuccess = false;
            bool connectSuccess = false;

            if (Connected) {
                System.Console.WriteLine($"Already connecting to {_ip}-{_port}, please don't connect repeatedly.");
                return false;
            }
            System.Console.WriteLine($"Connecting to {_ip}-{_port}");
            // 1. check ping
            pingSuccess = PingHost(_ip);
            if (pingSuccess) {
                // 2. check socketResult
                try {
                    socketClient = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socketClient.ReceiveTimeout = 500;
                    await socketClient.ConnectAsync(IPAddress.Parse(_ip), _port);
                    connectSuccess = true;
                } catch (Exception e) {
                    System.Console.WriteLine($"Error while connecting to {_ip}-{_port}: {e}");
                }
            } else {
                System.Console.WriteLine($"Failed to connect to {_ip}-{_port}");
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
                System.Console.WriteLine($"Ping error while pinging to {_ip}-{_port}: {pe}");
                return false;
            } finally {
                if (pinger != null) {
                    pinger.Dispose();
                }
            }
        }

        public void Connect() {
            if (ConnectToController()) {
                RunTask();
            }
        }
        
        public async Task ConnectAsync() {
            if (await ConnectToControllerAsync()) {
                RunTask();
            }
        }

        public void CloseConnection() {
            if (Connected) {
                CloseConnectionManually = true;
                socketClient.Close();
                Result = null;
                Commands.Clear();
                System.Console.WriteLine($"Close connection<{_ip}-{_port}> manually...");
            } else {
                System.Console.WriteLine($"Connection<{_ip}-{_port}> already closed...");
            }
        }

        public void SendCommand(string command) {
            Commands.Enqueue(command);
        }
        
        public async Task<string?> SendCommandAsync(string command) {
            SendCommand(command);

            string? result = null;
            while (Connected && result == null) {
                result = Result;
                if (result == null) {
                    await Task.Delay(LoopingInterval);
                }
            }
            Result = null;
            return result;
        }

        public async Task<Coordinates3D?> GetCurrentCoordinates() { 
            RetrieveResult = true;
            await Task.Delay(100);
            Coordinates3D? coordinates = _currentCoordinates;
            RetrieveResult = false;
            return coordinates;
        }

        private async Task<Coordinates3D?> GetCoordinatesAsync() {
            Coordinates3D? coordinates = null;
            if (Connected) {
                coordinates = new();
                string? x = await SendCommandAsync(_coordinateCommands[0]);
                if (x != null) {
                    coordinates.X = ParseResult(x);
                }
                string? y = await SendCommandAsync(_coordinateCommands[1]);
                if (y != null) {
                    coordinates.Y = ParseResult(y);
                }
                if (_coordinateCommands.Count() == 3) {
                    string? z = await SendCommandAsync(_coordinateCommands[2]);
                    if (z != null) {
                        coordinates.Z = ParseResult(z);
                    }
                }
                if (_currentCoordinates == null || !_currentCoordinates.Equals(coordinates)) {
                    System.Console.WriteLine($"coordinates: {coordinates.ToString()}");
                }
                if (_actionAfterReceiving.GetInvocationList().Length > 0) {
                    _actionAfterReceiving(coordinates);
                }
            }
            return coordinates;
        }

        public void RunLoop() {
            Task.Run(async () => {
                while (!CloseConnectionManually) {
                    if (RetrieveResult) {
                        _currentCoordinates = await GetCoordinatesAsync();
                    } else {
                        await Task.Delay(LoopingInterval);
                    }
                }
                System.Console.WriteLine($"................................");
            });
        }

        private int ParseResult(string result) {
            int coordinate = 0;
            if (result != null && result != "") {
                string lowData = result.Substring(6, 4);
                string HighData = result.Substring(10, 4);
                if (lowData != "ffff" && HighData != "ffff") {
                    coordinate = int.Parse((lowData), NumberStyles.HexNumber);
                    // coordinate = Convert.ToInt32(lowData, 16);
                }
            }
            return coordinate;
        }

        public static byte[] HexStrToBytes(string hexStr) => Enumerable.Range(0, hexStr.Length / 2)
            .Select(x => Convert.ToByte(hexStr.Substring(x * 2, 2), 16))
            .ToArray();

        public static string BytesToHexStr(byte[] bytes) => Convert.ToHexString(bytes).ToLower();
    }
}
