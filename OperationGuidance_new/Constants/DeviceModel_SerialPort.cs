using static OperationGuidance_new.Constants.DeviceConstants;

namespace OperationGuidance_new.Constants {
    public static class DeviceModel_SerialPort {
        public static string DEVICE_TYPE = DeviceTypes.SERIAL_PORT;
        public static SerialPortScanner Scanner { get; } = new();
    }

    public class SerialPortScanner: ADeviceModel {
        public SerialPortScanner() : base("Scanner") { }
    }
}
