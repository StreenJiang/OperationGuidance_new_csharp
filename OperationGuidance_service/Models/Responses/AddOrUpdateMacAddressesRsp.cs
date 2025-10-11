using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateMacAddressesRsp: ControlResponse {
        public MacAddressesDTO? MacAddressesDTO { get; set; }
    }
}
