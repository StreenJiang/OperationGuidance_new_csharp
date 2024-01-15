using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateDeviceModelRsp: HttpResponse {
        public DeviceModelDTO DeviceModelDTO { get; set;} = new();
    }
}
