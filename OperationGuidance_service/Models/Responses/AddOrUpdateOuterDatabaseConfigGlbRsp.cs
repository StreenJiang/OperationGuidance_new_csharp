using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateOuterDatabaseConfigGlbRsp: ControlResponse {
        public OuterDatabaseConfigGlbDTO OuterDatabaseConfigGlbDTO { get; set; } = new();
    }
}
