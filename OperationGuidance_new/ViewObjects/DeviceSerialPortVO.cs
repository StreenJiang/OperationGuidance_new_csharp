using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Constants;
using System.IO.Ports;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class DeviceSerialPortVO: AVOBase {
        [GridColumn("设备名称")]
        public string? name { get; set; }
        [GridColumn("设备描述")]
        public string? description { get; set; }
        [GridColumn("设备类型")]
        public string? str_type { get; set; }
        private int? _type_id;
        public int? type { 
            get => _type_id;
            set {
                _type_id = value;
                if (value != null) {
                    str_type = DeviceType_SerialPort.GetNameById(value.Value);
                }
            } 
        }
        [GridColumn("串口号")]
        public string? port_name { get; set; }
        [GridColumn("串口全名")]
        public string? port_full_name { get; set; }
        [GridColumn("波特率")]
        public int? baud_rate { get; set; }
        [GridColumn("数据位")]
        public int? data_bit { get; set; }
        [GridColumn("校验位")]
        public string? str_parity { get; set; }
        public int? _parity;
        public int? parity { 
            get => _parity; 
            set {
                _parity = value;
                if (value != null) {
                    str_parity = Enum.GetName(typeof(Parity), value);
                }
            }
        }
        [GridColumn("停止位")]
        public string? str_stop_bit { get; set; }
        public int? _stop_bit;
        public int? stop_bit { 
            get => _stop_bit; 
            set {
                _stop_bit = value;
                if (value != null) {
                    str_stop_bit = Enum.GetName(typeof(StopBits), value);
                }
            }
        }
        [GridColumn("数据类型")]
        public string? str_data_type { get; set; }
        public int? _data_type;
        public int? data_type { 
            get => _data_type; 
            set {
                _data_type = value;
                if (value != null) {
                    str_data_type = Enum.GetName(typeof(DataTypes), value);
                    switch (value) {
                        case 2:
                            str_data_type = "二进制";
                            break;
                        case 8:
                            str_data_type = "八进制";
                            break;
                        case 10:
                            str_data_type = "十进制";
                            break;
                        case 16:
                            str_data_type = "十六进制";
                            break;
                    }
                }
            }
        }
        [GridColumn("无效字符")]
        public string? invalid_char { get; set; }
        public int macs_id { get; set; }
    }
}
