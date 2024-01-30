using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceCommunicationReq: HttpRequest {
        public DeviceCommunicationDTO DeviceCommunicationDTO { get; set; }

        public AddOrUpdateDeviceCommunicationReq(DeviceCommunicationDTO deviceCommunicationDTO) {
            DeviceCommunicationDTO = deviceCommunicationDTO;
        }
    }
}
