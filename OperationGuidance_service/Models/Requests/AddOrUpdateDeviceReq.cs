using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceReq: HttpRequest {
        public DeviceDTO DeviceDTO { get; set; }

        public AddOrUpdateDeviceReq(DeviceDTO deviceDTO) {
            DeviceDTO = deviceDTO;
        }
    }
}
