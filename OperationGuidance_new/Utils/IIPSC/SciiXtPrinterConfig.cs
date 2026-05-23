using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Utils.IIPSC {
    public class SciiXtPrinterConfig: ConfigBase {
        // 供应商名称
        public string supplier_name { get; set; }
        // 项目名称
        public string project_name { get; set; }
        // 生产地点
        public string manufacture_location { get; set; }
        // 物料号
        public string part_number { get; set; }
        // 流水号
        [ConfigIgnore]
        public int sn { get; set; }

        // 文本行1格式
        public string text_1 { get; set; }
        // 文本行2格式
        public string text_2 { get; set; }
        // 文本行3格式
        public string text_3 { get; set; }
        // 文本行4格式
        public string text_4 { get; set; }
        // 文本行5格式
        public string text_5 { get; set; }

        // 二维码X
        public string sn_pos_x { get; set; }
        // 二维码Y
        public string sn_pos_y { get; set; }

        // 打印机名称
        public string printer_name { get; set; }
        public string second_printer_name { get; set; }
        // 第二条码长度校验
        public int second_barcode_length { get; set; }
        // 打印机配置是否弃用
        public int enabled { get; set; }
        public int enabled_second { get; set; }

        public SciiXtPrinterConfig() {
            enabled = (int) YesOrNo.NO;
            enabled_second = (int) YesOrNo.NO;

            printer_name = "";
            second_printer_name = "";
            second_barcode_length = 0;

            supplier_name = "SCII";
            project_name = "NE17";
            manufacture_location = "XA";
            part_number = "12296650";
            sn = 0;

            text_1 = "^FT50,90^A0N,33,31^FH\\^FD";   // supplier name
            text_2 = "^FT50,120^A0N,33,31^FH\\^FD";  // project name
            text_3 = "^FT50,150^A0N,33,31^FH\\^FD";  // date
            text_4 = "^FT50,180^A0N,33,31^FH\\^FD";  // serial number
            text_5 = "^FT50,220^A0N,33,31^FH\\^FD";  // traceability code

            sn_pos_x = "350";
            sn_pos_y = "50";
        }
    }
}
