using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateBrandRsp: HttpResponse {
        public BrandDTO BrandDTO { get; set;} = new();
    }
}
