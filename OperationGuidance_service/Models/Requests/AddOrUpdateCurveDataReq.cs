using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateCurveDataReq : HttpResponse {
        public CurveDataDTO CurveDataDTO { get; set; }

        public AddOrUpdateCurveDataReq(CurveDataDTO curveDataDTO) {
            CurveDataDTO = curveDataDTO;
        }
    }
}
