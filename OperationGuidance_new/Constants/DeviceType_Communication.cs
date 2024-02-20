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

        public static DeviceTypeCommunication? GetById(int id) {
            foreach (DeviceTypeCommunication type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            return null;
        }
        public static string? GetNameById(int id) {
            foreach (DeviceTypeCommunication type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            return null;
        }
        public static int? GetIdByName(string name) {
            foreach (DeviceTypeCommunication type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            return null;
        }
    }

    public class DeviceTypeCommunication: DeviceTypeBase {
        public DeviceTypeCommunication(int id, string name) : base(id, name) { }
    }

    public class CommunicationOpenProtocol: DeviceTypeCommunication {
        public CommunicationOpenProtocol() : base(1, "OpenProtocol") { }
    }

    public class CommunicationModBus: DeviceTypeCommunication {
        public CommunicationModBus() : base(2, "ModBus") { }
    }
}
