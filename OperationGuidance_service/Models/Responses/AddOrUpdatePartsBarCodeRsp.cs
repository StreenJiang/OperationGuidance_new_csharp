using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdatePartsBarCodeRsp: HttpResponse {
        public PartsBarCodeDTO PartsBarCodeDTO { get; set; }

        public AddOrUpdatePartsBarCodeRsp(PartsBarCodeDTO partsBarCodeDTO) => PartsBarCodeDTO = partsBarCodeDTO;
    }
}
