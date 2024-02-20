namespace OperationGuidance_new.Constants {
    public class DeviceType_SerialPort {
        public static List<DeviceTypeSerialPort> Elements = new();
        private static T AddNew<T>() where T: DeviceTypeSerialPort, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory Category => DeviceCategories.SERIAL_PORT;
        public static SerialPortScanner Scanner { get; } = AddNew<SerialPortScanner>();

        public static DeviceTypeSerialPort? GetById(int id) {
            foreach (DeviceTypeSerialPort type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            return null;
        }
        public static string? GetNameById(int id) {
            foreach (DeviceTypeSerialPort type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            return null;
        }
        public static int? GetIdByName(string name) {
            foreach (DeviceTypeSerialPort type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            return null;
        }
    }

    public class DeviceTypeSerialPort: DeviceTypeBase {
        public DeviceTypeSerialPort(int id, string name) : base(id, name) {}
    }

    public class SerialPortScanner: DeviceTypeSerialPort {
        public SerialPortScanner() : base(1, "Scanner") { }
    }
}
