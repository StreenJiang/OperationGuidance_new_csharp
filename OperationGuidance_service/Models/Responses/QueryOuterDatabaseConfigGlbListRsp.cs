using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryOuterDatabaseConfigGlbListRsp: ControlResponse {
        public List<OuterDatabaseConfigGlbDTO> OuterDatabaseConfigGlbDTOs {
            get; set;
        }
    }
}
