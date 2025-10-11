using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateOuterDatabaseConfigGlbReq: ControlRequest {
        public OuterDatabaseConfigGlbDTO OuterDatabaseConfigGlbDTO { get; set; }

        public AddOrUpdateOuterDatabaseConfigGlbReq(OuterDatabaseConfigGlbDTO dto) {
            OuterDatabaseConfigGlbDTO = dto;
        }
    }
}
