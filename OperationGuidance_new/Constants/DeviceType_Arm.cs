namespace OperationGuidance_new.Constants {
    public class DeviceType_Arm {
        public static List<DeviceTypeBase> Elements = new();
        private static T AddNew<T>() where T: DeviceTypeBase, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory DEVICE_TYPE = DeviceCategories.ARM;
        public static ArmCF01 CF01 { get; } = AddNew<ArmCF01>();
        public static ArmCF03 CF03 { get; } = AddNew<ArmCF03>();

        public static DeviceTypeArm? GetById(int id) {
            foreach (DeviceTypeArm type in Elements) {
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

    public class DeviceTypeArm: DeviceTypeBase {
        public Command COMMAND_READ_X_HEX;
        public Command COMMAND_READ_Y_HEX;
        public Command? COMMAND_READ_Z_HEX;
        public DeviceTypeArm(int id, string name, string[] commands) : base(id, name) {
            COMMAND_READ_X_HEX = new(commands[0]);
            COMMAND_READ_Y_HEX = new(commands[1]);
            Commands.Add(COMMAND_READ_X_HEX);
            Commands.Add(COMMAND_READ_Y_HEX);
            if (commands.Length == 3) {
                COMMAND_READ_Z_HEX = new(commands[2]);
                Commands.Add(COMMAND_READ_Z_HEX);
            }
        }
    }

    public class ArmCF01: DeviceTypeArm {
        public ArmCF01() : base(1, "CF01", new string[] { "010300030002340B", "0203000300023438" }) { }
    }

    public class ArmCF03: DeviceTypeArm {
        public ArmCF03() : base(2, "CF03", new string[] { "010300030002340B", "0203000300023438", "030300000002c5e9" }) { }
    }
}
