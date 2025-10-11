using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateCurveDataRsp: ControlResponse {
        public CurveDataDTO CurveDataDTO { get; set; }

        public AddOrUpdateCurveDataRsp(CurveDataDTO curveDataDTO) {
            CurveDataDTO = curveDataDTO;
        }
    }
}
