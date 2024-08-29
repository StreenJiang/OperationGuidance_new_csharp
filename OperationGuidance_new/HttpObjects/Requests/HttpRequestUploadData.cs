using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests {
    public class HttpRequestUploadData: HttpRequestBase_WHYC {
        public string? QrCode { get; set; }
        public string? Line { get; set; }
        public string? StationName { get; set; }
        public string? StaffName { get; set; }
        public string? MatCode { get; set; }
        // Should be torsion, not trosion. Customer made it wrong, so I keep it wrong
        public string? Trosion { get; set; }
        public string? TrosionStd { get; set; }
        public string? TrosionUp { get; set; }
        // This should be down, not dow. Same reason as below
        public string? TrosionDow { get; set; }
        public string? Time { get; set; }
        public string? Circle { get; set; }
        public string? Angle { get; set; }
        public string? Result { get; set; }
        public string? Error { get; set; }
        public string? CreateTime { get; set; }
        public int? Seq { get; set; }
        public int? SumQty { get; set; }
    }
}
