using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdatePartsBarCodeReq: ControlRequest {
        public PartsBarCodeDTO PartsBarCodeDTO { get; set; }

        public AddOrUpdatePartsBarCodeReq(PartsBarCodeDTO partsBarCodeDTO) => PartsBarCodeDTO = partsBarCodeDTO;
    }
}
