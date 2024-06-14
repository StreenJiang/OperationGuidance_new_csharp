namespace OperationGuidance_new.Constants {
    public class CurveDataTemp {
        public string result_data_identifier { get; set; }
        public string time_stamp { get; set; }
        public int data_type { get; set; }
        public string data_samples { get; set; }

        public CurveDataTemp(string result_data_identifier, string time_stamp, int data_type, string data_samples) {
            this.result_data_identifier = result_data_identifier;
            this.time_stamp = time_stamp;
            this.data_type = data_type;
            this.data_samples = data_samples;
        }
    }
}
