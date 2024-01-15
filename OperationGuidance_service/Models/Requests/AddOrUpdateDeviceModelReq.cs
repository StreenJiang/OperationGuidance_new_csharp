using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceModelReq: HttpRequest {
        public DeviceModelDTO DeviceModelDTO { get; set; }

        public AddOrUpdateDeviceModelReq(DeviceModelDTO deviceMOdelDTO) {
            DeviceModelDTO = deviceMOdelDTO;
        }
    }
}
