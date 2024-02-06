using OperationGuidance_service.Constants;
using System.IO.Ports;
using System.Management;

namespace OperationGuidance_service.Utils {
    public class ConnectionUtils {
        public static ConnectionStatus CheckConnection(string ip, int port) {
            return ConnectionStatus.CONNECTED;
        }

        public static Dictionary<string, string> GetSerialPorts() {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_PnPEntity WHERE Caption like '%(COM%'")) {
                // Console.WriteLine("==================================================");
                IEnumerable<string?> portFullNames = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());
                Dictionary<string, string> portsDict = new();
                foreach (string portName in SerialPort.GetPortNames()) {
                    string? portFullName = portFullNames.FirstOrDefault(port => port != null && port.Contains($"({portName})"));
                    if (portFullName != null) {
                        portsDict.Add(portName, portFullName);
                    }
                }

                // Console.WriteLine("ports: ");
                // int index = 1;
                // foreach (KeyValuePair<string, string> pair in portsDict) {
                //     System.Console.WriteLine($"{index++}. {pair.Key} - {pair.Value}");
                // }
                // Console.WriteLine("==================================================");
                return portsDict;
            }
        }
    }
}
