using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class FindOuterDatabaseConfigGlbForCheckingRsp: HttpResponse {
        public List<OuterDatabaseConfigGlbDTO> outerDTOs { get; set; } = new();
    }
}
