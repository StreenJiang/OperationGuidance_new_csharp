using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateDeviceArmRsp: HttpResponse {
        public DeviceArmDTO DeviceArmDTO { get; set;} = new();
    }
}
