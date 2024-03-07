using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class WorkstationVO: AVOBase {
        [GridColumn("站点名称")]
        public string? name { get; set; }

        public int? tool_id { get; set; }
        [GridColumn("工具名称")]
        public string? tool_name { get; set; }
        [GridColumn("工具类型")]
        public string? str_tool_type { get; set; }
        private int? _tool_type;
        public int? tool_type { 
            get => _tool_type; 
            set {
                _tool_type = value;
                if (value != null) {
                    str_tool_type = DeviceType_Tool.GetNameById(value.Value);
                }
            }
        }
        [GridColumn("工具IP")]
        public string? tool_ip { get; set; }
        [GridColumn("工具端口")]
        public int? tool_port { get; set; }

        public int? arm_id { get; set; }
        [GridColumn("力臂名称")]
        public string? arm_name { get; set; }
        [GridColumn("力臂型号")]
        public string? str_arm_type { get; set; }
        private int? _arm_type;
        public int? arm_type { 
            get => _arm_type; 
            set {
                _arm_type = value;
                if (value != null) {
                    str_arm_type = DeviceType_Arm.GetNameById(value.Value);
                }
            }
        }
        [GridColumn("力臂IP")]
        public string? arm_ip { get; set; }
        [GridColumn("力臂端口")]
        public int? arm_port { get; set; }

        public int? serial_port_id { get; set; }
        [GridColumn("串口设备名称")]
        public string? serial_port_name { get; set; }
        [GridColumn("串口设备描述")]
        public string? serial_port_description { get; set; }
        [GridColumn("串口设备类型")]
        public string? str_serial_port_type { get; set; }
        private int? _serial_port_type;
        public int? serial_port_type { 
            get => _serial_port_type; 
            set {
                _serial_port_type = value;
                if (value != null) {
                    str_serial_port_type = DeviceType_SerialPort.GetNameById(value.Value);
                }
            }
        }
        [GridColumn("串口号")]
        public string? serial_port_port_name { get; set; }
        [GridColumn("波特率")]
        public int? serial_port_baud_rate { get; set; }
        [GridColumn("数据位")]
        public int? serial_port_data_bit { get; set; }
        [GridColumn("校验位")]
        public int? serial_port_parity { get; set; }
        [GridColumn("停止位")]
        public int? serial_port_stop_bit { get; set; }
        [GridColumn("数据类型")]
        public int? serial_port_data_type { get; set; }
        
        public int? communication_id { get; set; }
        [GridColumn("通讯设备名称")]
        public string? communication_name { get; set; }
        [GridColumn("通讯设备描述")]
        public string? communication_description { get; set; }
        [GridColumn("通讯设备类型")]
        public string? str_communication_type { get; set; }
        private int? _communication_type;
        public int? communication_type { 
            get => _communication_type; 
            set {
                _communication_type = value;
                if (value != null) {
                    str_communication_type = DeviceType_Communication.GetNameById(value.Value);
                }
            }
        }
        [GridColumn("通讯设备IP")]
        public string? communication_ip { get; set; }
        [GridColumn("通讯设备端口")]
        public int? communication_port { get; set; }
    }
}
