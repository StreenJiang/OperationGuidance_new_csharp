namespace OperationGuidance_new.Utils {
    public class MESMessage_TZYX {
        public string StationCode { get; set; }
        public string BarCode { get; set; }
        public string Product { get; set; }
        public string Operator { get; set; }
        public bool Result { get; set; }
        public List<MESMessageData_TZYX> Data { get; set; } = new();
    }
}
