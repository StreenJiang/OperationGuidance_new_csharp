using static OperationGuidance_new.Constants.DeviceConstants;

namespace OperationGuidance_new.Constants {
    public static class DeviceModel_Tool {
        public static string DEVICE_TYPE = DeviceTypes.TOOL;
        public static ToolPF4000 PF4000 { get; } = new();
        public static ToolPF6000OP PF6000_OP { get; } = new();
        public static ToolSudongX7 SudongX7 { get; } = new();
    }

    public class ToolPF4000: ADeviceModel {
        public ToolPF4000() : base("PF4000") { }
        public static Command COMMAND_CONNECT_ASCII         = new("00200001003         \x00");
        public static Command COMMAND_DATA_ASCII            = new("002000600011        \x00");
        public static Command COMMAND_HEART_ASCII           = new("00209999001         \x00");
        public static Command COMMAND_PSET_ASCII            = new("00230018001         {0}\x00");
        public static Command COMMAND_SEND_BARCODE_ASCII    = new("002801500010    00  {0}\x00");
    }

    public class ToolPF6000OP: ADeviceModel {
        public ToolPF6000OP() : base("PF6000") { }
        public static Command COMMAND_CONNECT_ASCII         = new("00200001006         \x00");
        public static Command COMMAND_DATA_ASCII            = new("002000600011        \x00");
        public static Command COMMAND_HEART_ASCII           = new("00209999001         \x00");
        public static Command COMMAND_PSET_ASCII            = new("00230018001         {0}\x00");
        public static Command COMMAND_SEND_BARCODE_ASCII    = new("002801500010    00  {0}\x00");
    }

    public class ToolSudongX7: ADeviceModel {
        public ToolSudongX7() : base("SudongX7") { }
        public static Command COMMAND_CONNECT_ASCII         = new("00200001006         \x00");
        //public static Command COMMAND_DATA_ASCII            = new("002000600011        \x00");
        //public static Command COMMAND_HEART_ASCII           = new("00209999001         \x00");
        public static Command COMMAND_PSET_ASCII            = new("00230018001         {0}\x00");
        //public static Command COMMAND_SEND_BARCODE_ASCII    = new("002801500010    00  {0}\x00");
    }
}
