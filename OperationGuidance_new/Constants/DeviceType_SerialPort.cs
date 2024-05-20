namespace OperationGuidance_new.Constants {
    public class DeviceType_SerialPort {
        public static List<DeviceTypeSerialPort> Elements = new();
        private static T AddNew<T>() where T : DeviceTypeSerialPort, new() {
            T type = new();
            if (Elements.Find(e => e.Id == type.Id) != null) {
                throw new InvalidDataException($"Duplicated Id for type {typeof(DeviceType_SerialPort).Name}");
            }
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory Category => DeviceCategories.SERIAL_PORT;
        public static SerialPortScanner Scanner { get; } = AddNew<SerialPortScanner>();

        public static DeviceTypeSerialPort GetById(int id) {
            foreach (DeviceTypeSerialPort type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            throw new NullReferenceException($"Can't find type of serial port by type_id = {id}");
        }
        public static string GetNameById(int id) {
            foreach (DeviceTypeSerialPort type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            throw new NullReferenceException($"Can't find type of serial port by type_id = {id}");
        }
        public static int GetIdByName(string name) {
            foreach (DeviceTypeSerialPort type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            throw new NullReferenceException($"Can't find type of serial port by type_name = {name}");
        }
    }

    public class DeviceTypeSerialPort: DeviceTypeBase {
        public DeviceTypeSerialPort(int id, string name) : base(id, name) { }
    }

    public class SerialPortScanner: DeviceTypeSerialPort {
        public SerialPortScanner() : base(1, "Scanner") { }
    }
}
