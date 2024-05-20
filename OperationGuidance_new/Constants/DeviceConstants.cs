using log4net;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Constants {
    public class DeviceConstants {
    }

    // This is for DeviceBlock
    public static class DeviceCategories {
        public static List<DeviceCategory> Elements = new();
        private static T AddNew<T>() where T: DeviceCategory, new() {
            T type = new();
            Elements.Add(type);
            return type;
        }

        public static DeviceCategory TOOL = AddNew<TOOL>();
        public static DeviceCategory ARM = AddNew<ARM>();
        public static DeviceCategory COMMUNICATION = AddNew<COMMUNICATION>();
        public static DeviceCategory SERIAL_PORT = AddNew<SERIAL_PORT>();
        public static DeviceCategory IOBOX_ARRANGER = AddNew<IOBOX_ARRANGER>();
        public static DeviceCategory IOBOX_SETTERSELECTOR = AddNew<IOBOX_SETTERSELECTOR>();

        public static string? GetNameById(int id) {
            foreach (DeviceCategory type in Elements) {
                if (type.Id == id) {
                    return type.Name;
                }
            }
            return null;
        }
        public static int? GetIdByName(string name) {
            foreach (DeviceCategory type in Elements) {
                if (type.Name == name) {
                    return type.Id;
                }
            }
            return null;
        }
    }

    public class DeviceCategory {
        public int Id;
        public string Name;
        public string IconStr;
        public string IconErrorStr;
        public string IconEmptyStr;
        public DeviceCategory(int id, string name, Image icon, Image iconError, Image iconEmpty) {
            Id = id;
            Name = name;
            IconStr = CommonUtils.ImageToBase64(icon);
            IconErrorStr = CommonUtils.ImageToBase64(iconError);
            IconEmptyStr = CommonUtils.ImageToBase64(iconEmpty);
        }
    }

    public class TOOL: DeviceCategory {
        public TOOL(): base(1, "工具", 
            Properties.Resources.aneng_screw_gun, 
            Properties.Resources.aneng_screw_gun_error, 
            Properties.Resources.aneng_screw_gun_empty) {}
    }
    public class ARM: DeviceCategory {
        public ARM(): base(2, "力臂", 
            Properties.Resources.aneng_arm,
            Properties.Resources.aneng_arm_error,
            Properties.Resources.aneng_arm_empty) {}
    }
    public class SERIAL_PORT: DeviceCategory {
        public SERIAL_PORT(): base(3, "串口设备", 
            Properties.Resources.aneng_serial_port,
            Properties.Resources.aneng_serial_port_error,
            Properties.Resources.aneng_serial_port_empty) {}
    }
    public class COMMUNICATION: DeviceCategory {
        public COMMUNICATION(): base(4, "通讯设备", 
            Properties.Resources.aneng_communication,
            Properties.Resources.aneng_communication_error,
            Properties.Resources.aneng_communication_empty) {}
    }
    public class IOBOX_ARRANGER: DeviceCategory {
        public IOBOX_ARRANGER(): base(5, "排列机", 
            Properties.Resources.aneng_feeder,
            Properties.Resources.aneng_feeder_error,
            Properties.Resources.aneng_feeder_empty) { }
    }
    public class IOBOX_SETTERSELECTOR: DeviceCategory {
        public IOBOX_SETTERSELECTOR(): base(6, "套筒选择器", 
            Properties.Resources.aneng_setter_selector,
            Properties.Resources.aneng_setter_selector_error,
            Properties.Resources.aneng_setter_selector_empty) { }
    }

    public class DeviceTypeBase {
        protected ILog logger;
        public List<Command> Commands;
        public int Id { get; }
        public string Name { get; }
        public DeviceTypeBase(int id, string name) {
            logger = MainUtils.GetLogger(GetType());
            Id = id;
            Name = name;
            Commands = new();
        }
    }

    public enum DeviceStatus {
        NORMAL,
        ERROR,
        EMPTY,
    }
}
