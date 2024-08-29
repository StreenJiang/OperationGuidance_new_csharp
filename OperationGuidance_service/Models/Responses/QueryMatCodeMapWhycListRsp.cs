using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryMatCodeMapWhycListRsp: HttpResponse {
        public List<MatCodeMapWhycDTO> MatCodeMapWhycDTOs {
            get; set;
        }
    }
}
