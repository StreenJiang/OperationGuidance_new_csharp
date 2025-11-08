using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Utils.IIPSC {
    public class SciiXtPrinterConfig: ConfigBase {
        // 10位零部件编码
        public string part_code { get; set; }
        // 6位供应商编码
        public string vender_code { get; set; }
        // 8位批次编码
        [ConfigIgnore]
        public string batch_code { get; set; }
        // 流水号
        [ConfigIgnore]
        public int sn { get; set; }
        // 软件版本
        public string soft_version { get; set; }
        // 硬件版本
        public string hard_version { get; set; }

        // 文本行1格式
        public string text_1 { get; set; }
        // 文本行2格式
        public string text_2 { get; set; }
        // 文本行3格式
        public string text_3 { get; set; }
        // 文本行4格式
        public string text_4 { get; set; }
        // 文本位置Y4
        public string location_y { get; set; }

        // 二维码X
        public string sn_pos_x { get; set; }
        // 二维码Y
        public string sn_pos_y { get; set; }

        // 打印机名称
        [ConfigIgnore]
        public string printer_name { get; set; }
        // 打印机配置是否弃用
        public int enabled { get; set; }

        public SciiXtPrinterConfig() {
            enabled = (int) YesOrNo.NO;
            printer_name = "";

            part_code = "7161620072";
            vender_code = "777168";
            batch_code = "";
            sn = 0;
            soft_version = "V1.0.0";
            hard_version = "HW3.2";

            text_1 = "^FT50,130^A0N,33,31^FH\\^FD";
            text_2 = "^FT50,160^A0N,33,31^FH\\^FD";
            text_3 = "^FT50,190^A0N,33,31^FH\\^FD";
            text_4 = "^FT50,220^A0N,33,31^FH\\^FD";
            location_y = "^FO0,30^GFA,22528,22528,00088,:Z64:eJztlzFqwzAUhlVMJg+ecpoMHnoBQ9Hco/hKhoweGugZeg9DusmgSrItO7SQwb+hw/cNTsjw8fPy/PRkDADAMwrvB+8rP1Tej8bU3t+K8Mu41/tibfNlT7YRey9937m+7Ltg9MaExyjxnkNeG/NO3nbytru99VLfoI0lCMjypvpqvYVzbnCudPdoHGqVN7TDlPdN6638XN+7H+vZ+ynwnta8Um/Zz/17jW9c8I7CvDblnb2tfxX0b73Wd4j/lzfp6+6857W+Uu+ld33q32t4K+J4MJVkPmzyLt5vhXdT32mcmTiStXmV3n7t3zQhVV679m/0trN3d//6bX3DmybNm+eZzuuW+ZvOi7s+b6P15voOxUF5td7cv12Kru/fJq4O2avr3yEddQfUV+vN87eLvnC4had0njVab56/4cMkr/h8e9d6L0v/fhyQ1+a87ezd3b/VY31rf9PkPT3WV+Yt/9h/xfuk1Fs8nG//f1832/0hXTpF3u3+sHrF9zfxfTOfx0JvsczffO8e9ioTv+/zGq9ZREVarKeNUoGNR8UBXgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgKf8AMeeF9w=:8CAB";

            sn_pos_x = "350";
            sn_pos_y = "50";
        }
    }
}
