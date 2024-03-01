using OperationGuidance_service.Constants;
using System.IO.Ports;
using WmiLight;

namespace OperationGuidance_service.Utils {
    public class ConnectionUtils {
        public static ConnectionStatus CheckConnection(string ip, int port) {
            return ConnectionStatus.CONNECTED;
        }

        public static Dictionary<string, string> GetSerialPorts() {
            using (WmiConnection con = new WmiConnection()) {
                WmiQuery wmiQuery = con.CreateQuery("SELECT Caption FROM Win32_PnPEntity WHERE Caption like '%(COM%'");
                IEnumerable<string?> portFullNames = wmiQuery.ToList().Select(p => p["Caption"].ToString());
                Dictionary<string, string> portsDict = new();
                foreach (string portName in SerialPort.GetPortNames()) {
                    string? portFullName = portFullNames.FirstOrDefault(port => port != null && port.Contains($"({portName})"));
                    if (portFullName != null) {
                        portsDict.Add(portName, portFullName);
                    }
                }

                // Console.WriteLine("==================================================");
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
