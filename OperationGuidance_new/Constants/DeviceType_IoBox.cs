using log4net;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public class DeviceType_IoBox {
        public static List<DeviceTypeBase> Elements = new();
        private static T AddNew<T>() where T : DeviceTypeBase, new() {
            T type = new();
            if (Elements.Find(e => e.Id == type.Id) != null) {
                throw new InvalidDataException($"Duplicated Id for type {typeof(DeviceType_IoBox).Name}");
            }
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory Category = DeviceCategories.IOBOX;
        public static IoBoxSetterSelector_4 SetterSelector_4 { get; } = AddNew<IoBoxSetterSelector_4>();
        public static IoBoxSetterSelector_8 SetterSelector_8 { get; } = AddNew<IoBoxSetterSelector_8>();
        public static IoBoxArranger Arranger { get; } = AddNew<IoBoxArranger>();

        public static DeviceTypeIoBox GetById(int id) {
            foreach (DeviceTypeIoBox type in Elements) {
                if (type.Id == id) {
                    return type;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_id = {id}");
        }
        public static string GetNameById(int id) {
            foreach (DeviceTypeIoBox type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_id = {id}");
        }
        public static int GetIdByName(string name) {
            foreach (DeviceTypeIoBox type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            throw new NullReferenceException($"Can't find type of tool by type_name = {name}");
        }
    }

    public abstract class DeviceTypeIoBox: DeviceTypeBase {
        protected ILog logger;
        private Command? _command_write;

        public Command? COMMAND_READ;
        public Command? COMMAND_WRITE(string writeMsg) {
            if (_command_write != null) {
                string temp = _command_write.GetMessage(writeMsg);
                byte[] bytes = MainUtils.ToBytes(temp);
                return new(temp + MainUtils.Crc16ToString(bytes));
            }
            return null;
        }
        public DeviceTypeIoBox(int id, string name) : base(id, name) {
            logger = MainUtils.GetLogger(GetType());
            COMMAND_READ = new("0903000000044541");
            _command_write = new("0906000000{0}");
        }

        public abstract string? AnalyzeData(string dataMessage, Action? actionAfterAnalysis = null, int? deviceId = null);
    }

    public abstract class IoBoxSetterSelector: DeviceTypeIoBox {
        public IoBoxSetterSelector(int id, string name) : base(id, name) { }
    }

    public class IoBoxSetterSelector_4: IoBoxSetterSelector {
        public int SetterNum { get; set; } = 4;

        public IoBoxSetterSelector_4() : base(1, "SetterSelector_4") { }

        public override string? AnalyzeData(string dataMessage, Action? actionAfterAnalysis = null, int? deviceId = null) {
            string? result = null;
            return result;
        }
    }

    public class IoBoxSetterSelector_8: IoBoxSetterSelector {
        public int SetterNum { get; set; } = 8;

        public IoBoxSetterSelector_8() : base(2, "SetterSelector_8") { }

        public override string? AnalyzeData(string dataMessage, Action? actionAfterAnalysis = null, int? deviceId = null) {
            string? result = null;
            return result;
        }
    }

    public class IoBoxArranger: DeviceTypeIoBox {
        public IoBoxArranger() : base(3, "Arranger") { }

        public override string? AnalyzeData(string dataMessage, Action? actionAfterAnalysis = null, int? deviceId = null) {
            string? result = null;
            return result;
        }
    }
}
