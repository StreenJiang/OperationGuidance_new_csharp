using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateBrandReq: HttpRequest {
        public BrandDTO BrandDTO { get; set; }

        public AddOrUpdateBrandReq(BrandDTO brandDTO) {
            BrandDTO = brandDTO;
        }
    }
}
