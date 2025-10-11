using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class FindMatCodeMapByMatCodeRsp: ControlResponse {
        public MatCodeMapWhycDTO? MatCodeMapWhycDTO { get; set; }
    }
}
