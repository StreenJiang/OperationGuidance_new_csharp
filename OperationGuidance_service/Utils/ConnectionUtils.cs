using OperationGuidance_service.Database;
using System.Collections;
using System.Globalization;
using System.IO.Ports;
using System.Resources;
using WmiLight;

namespace OperationGuidance_service.Utils {
    public class ConnectionUtils {
        public static bool HealthChecked = false;

        public static List<String> GetResourcesFileNames() {
            List<String> fileNames = new();

            // Need to call this first otherwise can't get resource set
            string init_mysql = Resource.init_mysql;

            ResourceSet? resourceSet = Resource.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, false, false);
            if (resourceSet != null) {
                fileNames = resourceSet.Cast<DictionaryEntry>().Select(entry => entry.Key).Cast<String>().ToList();
                fileNames.Sort();
            }

            return fileNames;
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
