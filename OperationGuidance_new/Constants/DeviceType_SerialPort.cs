namespace OperationGuidance_new.Constants {
    public class DeviceType_SerialPort {
        public static List<DeviceSerialPort> Elements = new();
        private static T AddNew<T>() where T: DeviceSerialPort, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory Category => DeviceCategories.SERIAL_PORT;
        public static SerialPortScanner Scanner { get; } = AddNew<SerialPortScanner>();

        public static DeviceSerialPort? GetById(int id) {
            foreach (DeviceSerialPort type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            return null;
        }
        public static string? GetNameById(int id) {
            foreach (DeviceSerialPort type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            return null;
        }
        public static int? GetIdByName(string name) {
            foreach (DeviceSerialPort type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            return null;
        }
    }

    public class DeviceSerialPort: DeviceTypeBase {
        public DeviceSerialPort(int id, string name) : base(id, name) {}
    }

    public class SerialPortScanner: DeviceSerialPort {
        public SerialPortScanner() : base(1, "Scanner") { }
    }
}
