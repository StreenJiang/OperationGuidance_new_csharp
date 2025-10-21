namespace OperationGuidance_service.Models.DTOs {
    public class SCII_XT_UserPermissionDTO {
        public string message { get; set; } = string.Empty;

        public List<string> menuPermissions { get; set; } = new();
        public List<string> buttonPermissions { get; set; } = new();
    }
}
