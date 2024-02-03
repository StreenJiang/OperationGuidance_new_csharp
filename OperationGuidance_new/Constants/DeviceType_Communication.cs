namespace OperationGuidance_new.Constants {
    public class DeviceType_Communication {
        public static List<DeviceTypeBase> Elements = new();
        private static T AddNew<T>() where T: DeviceTypeBase, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory DEVICE_TYPE = DeviceCategories.COMMUNICATION;
        public static CommunicationOpenProtocol OpenProtocol { get; } = AddNew<CommunicationOpenProtocol>();
        public static CommunicationModBus ModBus { get; } = AddNew<CommunicationModBus>();

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

    public class CommunicationOpenProtocol: DeviceTypeBase {
        public CommunicationOpenProtocol() : base(1, "OpenProtocol") { }
        public static Command COMMAND_CONNECT_ASCII         = new("");
    }

    public class CommunicationModBus: DeviceTypeBase {
        public CommunicationModBus() : base(2, "ModBus") { }
        public static Command COMMAND_CONNECT_ASCII         = new("");
    }
}
