namespace OperationGuidance_service.Models.Responses {
    public class ReimportProgressInfo {
        public int BatchCount { get; set; }
        public int TotalInserted { get; set; }
        public int LastId { get; set; }
        public int TotalBatches { get; set; }
        public int DeletedCount { get; set; }
        public int TotalToDelete { get; set; }
        public string? Phase { get; set; }
    }
}
