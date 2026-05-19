namespace OperationGuidance_service.Models.Responses {
    public class ReimportPartsBarcodeRsp {
        public int DeletedRows { get; set; }
        public int InsertedRows { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
