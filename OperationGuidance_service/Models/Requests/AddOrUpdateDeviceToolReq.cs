using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateDeviceToolReq: HttpRequest {
        public DeviceToolDTO DeviceToolDTO { get; set; }

        public AddOrUpdateDeviceToolReq(DeviceToolDTO dto) {
            DeviceToolDTO = dto;
        }
    }
}
