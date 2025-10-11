using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class FindCurveDataByOperationDataIdRsp: ControlResponse {
        public List<CurveDataDTO> CurveDataDTOs { get; set; }

        public FindCurveDataByOperationDataIdRsp(List<CurveDataDTO> curveDataDTOs) {
            CurveDataDTOs = curveDataDTOs;
        }
    }
}
