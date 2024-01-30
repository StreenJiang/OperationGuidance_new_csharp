using static OperationGuidance_new.Constants.DeviceConstants;

namespace OperationGuidance_new.Constants {
    public static class DeviceModel_Communication {
        public static string DEVICE_TYPE = DeviceTypes.COMMUNICATION;
        public static CommunicationOpenProtocol OpenProtocol { get; } = new();
        public static CommunicationModBus ModBus { get; } = new();
    }

    public class CommunicationOpenProtocol: ADeviceModel {
        public CommunicationOpenProtocol() : base("OpenProtocol") { }
        public static Command COMMAND_CONNECT_ASCII         = new("");
    }

    public class CommunicationModBus: ADeviceModel {
        public CommunicationModBus() : base("ModBus") { }
        public static Command COMMAND_CONNECT_ASCII         = new("");
    }
}
