namespace OperationGuidance_service.Models.DTOs {
    public class SCII_XT_OperatorLoginDTO {
        public bool loginSuccess { get; set; }
        public int userId { get; set; }
        public string message { get; set; } = string.Empty;
    }
}
