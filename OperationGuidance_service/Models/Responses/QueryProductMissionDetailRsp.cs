using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryProductMissionDetailRsp: HttpResponse {
        public ProductMissionDTO? ProductMissionDTO {
            get; set; 
        }
    }
}
