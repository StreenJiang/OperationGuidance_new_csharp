using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceArmReq: HttpRequest {
        public DeviceArmDTO DeviceArmDTO { get; set; }

        public AddOrUpdateDeviceArmReq(DeviceArmDTO deviceArmDTO) {
            DeviceArmDTO = deviceArmDTO;
        }
    }
}
