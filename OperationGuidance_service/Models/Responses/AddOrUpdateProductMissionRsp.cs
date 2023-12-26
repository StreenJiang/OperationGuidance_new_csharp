using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateProductMissionRsp: HttpResponse {
        public ProductMissionDTO ProductMissionDTO { get; set;} = new();
    }
}
