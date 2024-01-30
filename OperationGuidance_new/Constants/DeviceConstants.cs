namespace OperationGuidance_new.Constants {
    public class DeviceConstants {
        public static class DeviceTypes {
            public static string TOOL = "工具";
            public static string ARM = "力臂";
            public static string SERIAL_PORT = "串口设备";
            public static string COMMUNICATION = "通讯设备";
        }

        public abstract class ADeviceModel {
            public string NAME { get; }
            public ADeviceModel(string name) => NAME=name;
        }
    }
}
