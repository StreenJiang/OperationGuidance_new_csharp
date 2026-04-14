using log4net;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Database;
using System.Collections;
using System.Data.Common;
using System.Globalization;
using System.IO.Ports;
using System.Resources;
using WmiLight;

namespace OperationGuidance_service.Utils {
    public class ConnectionUtils {
        private static ILog log = LogManager.GetLogger(typeof(ConnectionUtils));
        public static bool HealthChecked = false;

        public static ConnectionStatus CheckConnection(string ip, int port) {
            return ConnectionStatus.CONNECTED;
        }

        public static bool CheckTableExists(DbConnection conn, string tableName)
            => CheckTableExists(conn, SystemUtils.GetDataBase(), tableName);

        public static bool CheckTableExists(DbConnection conn, string database, string tableName) {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(database)) {
                log.Warn($"Invalid table name or database name: {tableName}, {database}");
                return false;
            }

            bool exists;
            using DbCommand dbCommand = conn.CreateCommand();

            try {
                // ANSI SQL way.  Works in PostgreSQL, MSSQL, MySQL.  
                dbCommand.CommandText = $"select count(1) from information_schema.tables where table_schema = '{database}' and table_name = '{tableName}'";
                log.Info($"Checking table exists or not, sql: {dbCommand.CommandText}");

                object? result = dbCommand.ExecuteScalar();
                exists = result != null && Convert.ToInt32(result) > 0;

                log.Info($"Checking table exists or not, result: {exists}");
            } catch (Exception e) {
                log.Warn($"Checking table exists or not, catching exception, e = {e}");

                try {
                    // Other RDBMS.  Graceful degradation
                    dbCommand.CommandText = $"select 1 from {tableName} where 1 = 0";
                    log.Info($"Checking table exists or not inside catching block, sql: {dbCommand.CommandText}");

                    dbCommand.ExecuteNonQuery();
                    exists = true;

                    log.Info($"Checking table exists or not inside catching block, result: {exists}");
                } catch (Exception e1) {
                    log.Warn($"Checking table exists or not inside catching block, catching exception again, e1 = {e1}");

                    exists = false;
                }
            }

            return exists;
        }

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
