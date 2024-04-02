using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateMacAddressesReq: HttpRequest {
        public MacAddressesDTO MacAddressesDTO { get; set; }

        public AddOrUpdateMacAddressesReq(MacAddressesDTO dto) {
            MacAddressesDTO = dto;
        }
    }
}
