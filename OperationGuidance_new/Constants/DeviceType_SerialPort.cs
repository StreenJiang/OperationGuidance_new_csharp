namespace OperationGuidance_new.Constants {
    public class DeviceType_SerialPort {
        public static List<DeviceTypeBase> Elements = new();
        private static T AddNew<T>() where T: DeviceTypeBase, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory Category = DeviceCategories.SERIAL_PORT;
        public static SerialPortScanner Scanner { get; } = AddNew<SerialPortScanner>();

        public static DeviceTypeBase? GetById(int id) {
            foreach (DeviceTypeBase type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            return null;
        }
        public static string? GetNameById(int id) {
            foreach (DeviceTypeBase type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            return null;
        }
        public static int? GetIdByName(string name) {
            foreach (DeviceTypeBase type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            return null;
        }
    }

    public class SerialPortScanner: DeviceTypeBase {
        public SerialPortScanner() : base(1, "Scanner") { }
    }
}
