using System.Globalization;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Constants {
    public class DeviceType_Arm {
        public static List<DeviceTypeBase> Elements = new();
        private static T AddNew<T>() where T : DeviceTypeBase, new() {
            T type = new();
            if (Elements.Find(e => e.Id == type.Id) != null) {
                throw new InvalidDataException($"Duplicated Id for type {typeof(DeviceType_Arm).Name}");
            }
            Elements.Add(type);
            return type;
        }

        public static ArmCF01 CF01 { get; } = AddNew<ArmCF01>();
        public static ArmCF02 CF02 { get; } = AddNew<ArmCF02>();
        // public static ArmCF03 CF03 { get; } = AddNew<ArmCF03>();

        public static DeviceTypeArm GetById(int id) {
            foreach (DeviceTypeArm type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            throw new NullReferenceException($"Can't find type of arm by type_id = {id}");
        }
        public static string GetNameById(int id) {
            foreach (DeviceTypeBase type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            throw new NullReferenceException($"Can't find type of arm by type_id = {id}");
        }
        public static int GetIdByName(string name) {
            foreach (DeviceTypeBase type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            throw new NullReferenceException($"Can't find type of arm by type_name = {name}");
        }
    }

    public class DeviceTypeArm: DeviceTypeBase {
        public Command COMMAND_READ_X_HEX;
        public Command COMMAND_READ_Y_HEX;
        public Command? COMMAND_READ_Z_HEX;
        public int MaxValue;
        public DeviceTypeArm(int id, string name, string[] commands, int maxValue) : base(id, name) {
            COMMAND_READ_X_HEX = new(commands[0]);
            COMMAND_READ_Y_HEX = new(commands[1]);
            Commands.Add(COMMAND_READ_X_HEX);
            Commands.Add(COMMAND_READ_Y_HEX);
            if (commands.Length == 3) {
                COMMAND_READ_Z_HEX = new(commands[2]);
                Commands.Add(COMMAND_READ_Z_HEX);
            }
            MaxValue = maxValue;
        }

        public virtual void AnalyzeData(string x, string y, string? z, Action<int, Coordinates3D>? _ioBoxActionAfterAnalysis, int deviceId) {
            if (_ioBoxActionAfterAnalysis != null) {
                Coordinates3D coordinates = new();
                if (!string.IsNullOrEmpty(x)) {
                    coordinates.X = ParseResult(x);
                }
                if (!string.IsNullOrEmpty(y)) {
                    coordinates.Y = ParseResult(y);
                }
                if (!string.IsNullOrEmpty(z)) {
                    coordinates.Z = ParseResult(z);
                }

                _ioBoxActionAfterAnalysis(MaxValue, coordinates);
            }
        }

        private int ParseResult(string result) {
            int coordinate = 0;
            if (result != null && result != "") {
                string lowData = result.Substring(6, 4);
                string HighData = result.Substring(10, 4);
                if (lowData != "ffff" && HighData != "ffff") {
                    coordinate = int.Parse(lowData, NumberStyles.HexNumber);
                    // coordinate = Convert.ToInt32(lowData, 16);
                }
            }
            return coordinate;
        }
    }

    public class ArmCF01: DeviceTypeArm {
        public ArmCF01() : base(1, "CF01", new string[] { "010300030002340B", "0203000300023438" }, 0) { }
    }

    public class ArmCF02: DeviceTypeArm {
        public ArmCF02() : base(2, "CF02", new string[] { "010300000001840A", "0203000000018439" }, 0) { }
    }

    public class ArmCF03: DeviceTypeArm {
        public ArmCF03() : base(3, "CF03", new string[] { "010300030002340B", "0203000300023438", "030300000002c5e9" }, 0) { }
    }
}
