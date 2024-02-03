namespace OperationGuidance_new.Constants {
    public class DeviceType_Tool {
        public static List<DeviceTypeBase> Elements = new();
        private static T AddNew<T>() where T: DeviceTypeBase, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory Category = DeviceCategories.TOOL;
        public static ToolPF4000 PF4000 { get; } = AddNew<ToolPF4000>();
        public static ToolPF6000OP PF6000_OP { get; } = AddNew<ToolPF6000OP>();
        public static ToolSudongX7 SudongX7 { get; } = AddNew<ToolSudongX7>();

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

    public class ToolPF4000: DeviceTypeBase {
        public ToolPF4000() : base(1, "PF4000") { }
        public static Command COMMAND_CONNECT_ASCII         = new("00200001003         \x00");
        public static Command COMMAND_DATA_ASCII            = new("002000600011        \x00");
        public static Command COMMAND_HEART_ASCII           = new("00209999001         \x00");
        public static Command COMMAND_PSET_ASCII            = new("00230018001         {0}\x00");
        public static Command COMMAND_SEND_BARCODE_ASCII    = new("002801500010    00  {0}\x00");
    }

    public class ToolPF6000OP: DeviceTypeBase {
        public ToolPF6000OP() : base(2, "PF6000-OP") { }
        public static Command COMMAND_CONNECT_ASCII         = new("00200001006         \x00");
        public static Command COMMAND_DATA_ASCII            = new("002000600011        \x00");
        public static Command COMMAND_HEART_ASCII           = new("00209999001         \x00");
        public static Command COMMAND_PSET_ASCII            = new("00230018001         {0}\x00");
        public static Command COMMAND_SEND_BARCODE_ASCII    = new("002801500010    00  {0}\x00");
    }

    public class ToolSudongX7: DeviceTypeBase {
        public ToolSudongX7() : base(3, "SudongX7") { }
        public static Command COMMAND_CONNECT_ASCII         = new("00200001006         \x00");
        //public static Command COMMAND_DATA_ASCII            = new("002000600011        \x00");
        //public static Command COMMAND_HEART_ASCII           = new("00209999001         \x00");
        public static Command COMMAND_PSET_ASCII            = new("00230018001         {0}\x00");
        //public static Command COMMAND_SEND_BARCODE_ASCII    = new("002801500010    00  {0}\x00");
    }
}
