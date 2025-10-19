namespace OperationGuidance_service.Models.AbstractClasses {
    public class SCII_XT_Response: HttpResponse {
        public int code { get; set; }
        public object? datalnfo { get; set; } = null;
        public string message { get; set; } = string.Empty;
    }
}
