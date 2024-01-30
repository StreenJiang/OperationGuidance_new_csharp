using static OperationGuidance_new.Constants.DeviceConstants;

namespace OperationGuidance_new.Constants {
    public static class DeviceModel_SerialPort {
        public static string DEVICE_TYPE = DeviceTypes.SERIAL_PORT;
        public static SerialPortCOM3 COM3 { get; } = new();
    }

    public class SerialPortCOM3: ADeviceModel {
        public SerialPortCOM3() : base("COM3") { }
    }
}
