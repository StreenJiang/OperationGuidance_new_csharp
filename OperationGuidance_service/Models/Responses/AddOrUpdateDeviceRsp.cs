using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateDeviceRsp: HttpResponse {
        public DeviceDTO DeviceDTO { get; set;} = new();
    }
}
