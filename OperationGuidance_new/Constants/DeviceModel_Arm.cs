using static OperationGuidance_new.Constants.DeviceConstants;

namespace OperationGuidance_new.Constants {
    public static class DeviceModel_Arm {
        public static string DEVICE_TYPE = DeviceTypes.ARM;
        public static ArmCF01 CF01 { get; } = new();
        public static ArmCF03 CF03 { get; } = new();
    }

    public class ArmCF01: ADeviceModel {
        public ArmCF01() : base("CF01") { }
        public static Command COMMAND_READ_X_HEX            = new("010300030002340B");
        public static Command COMMAND_READ_Y_HEX            = new("0203000300023438");
        //public static Command COMMAND_READ_Z_HEX            = new("030300000002c5e9");
    }

    public class ArmCF03: ADeviceModel {
        public ArmCF03() : base("CF03") { }
        public static Command COMMAND_READ_X_HEX            = new("010300030002340B");
        public static Command COMMAND_READ_Y_HEX            = new("0203000300023438");
        public static Command COMMAND_READ_Z_HEX            = new("030300000002c5e9");
    }
}
