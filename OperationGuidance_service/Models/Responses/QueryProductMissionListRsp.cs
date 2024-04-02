using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryProductMissionListRsp: HttpResponse {
        public List<ProductMissionDTO> ProductMissionDTOs { get; set; }
        public QueryProductMissionListRsp(List<ProductMissionDTO> productMissionDTOs) => ProductMissionDTOs = productMissionDTOs;
    }
}
