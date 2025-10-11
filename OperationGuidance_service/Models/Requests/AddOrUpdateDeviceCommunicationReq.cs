using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceCommunicationReq: ControlRequest {
        public DeviceCommunicationDTO DeviceCommunicationDTO { get; set; }

        public AddOrUpdateDeviceCommunicationReq(DeviceCommunicationDTO ato) {
            DeviceCommunicationDTO = ato;
        }
    }
}
