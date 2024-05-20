namespace OperationGuidance_new.Constants {
    public class DeviceType_Communication {
        public static List<DeviceTypeBase> Elements = new();
        private static T AddNew<T>() where T : DeviceTypeBase, new() {
            T type = new();
            if (Elements.Find(e => e.Id == type.Id) != null) {
                throw new InvalidDataException($"Duplicated Id for type {typeof(DeviceType_Communication).Name}");
            }
            Elements.Add(type);
            return type;
        }

        public static CommunicationOpenProtocol OpenProtocol { get; } = AddNew<CommunicationOpenProtocol>();
        public static CommunicationModBus ModBus { get; } = AddNew<CommunicationModBus>();

        public static DeviceTypeCommunication GetById(int id) {
            foreach (DeviceTypeCommunication type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            throw new NullReferenceException($"Can't find type of communication device by type_id = {id}");
        }
        public static string GetNameById(int id) {
            foreach (DeviceTypeCommunication type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            throw new NullReferenceException($"Can't find type of communication device by type_id = {id}");
        }
        public static int GetIdByName(string name) {
            foreach (DeviceTypeCommunication type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            throw new NullReferenceException($"Can't find type of communication device by type_name = {name}");
        }
    }

    public class DeviceTypeCommunication: DeviceTypeBase {
        public DeviceTypeCommunication(int id, string name) : base(id, name) { }
    }

    public class CommunicationOpenProtocol: DeviceTypeCommunication {
        public CommunicationOpenProtocol() : base(1, "OpenProtocol") { }
    }

    public class CommunicationModBus: DeviceTypeCommunication {
        public Command COMMAND_CHECK_STATUS = new("");

        public CommunicationModBus() : base(2, "ModBus") {
        }

        public string? AnalyzeData(string dataMessage, Action? actionAfterAnalysis = null) {
            return null;
        }
    }
}
