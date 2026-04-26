using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Configs {
    public class SciiXtConfig: ConfigBase {
        public string http_host { get; set; }
        public string procedure_code { get; set; }
        public string equipment_code { get; set; }
        public string batch_no { get; set; }
        public string recipe_code { get; set; }
        public string plc_is_ready_addr { get; set; }
        public string plc_register_addr { get; set; }
        public int send_upper_cover { get; set; } = (int) YesOrNo.NO;

        public SciiXtConfig() {
            http_host = "http://10.10.59.1:5400";
            procedure_code = "";
            equipment_code = "";
            batch_no = "";
            recipe_code = "";
            plc_is_ready_addr = "6000";
            plc_register_addr = "6002";
        }

        public enum PlcStatus {
            OK = 1,
            RSP = 99,
        }

        public enum PlcResult {
            OK = 1,
            NOK = 2,
        }
    }
}
