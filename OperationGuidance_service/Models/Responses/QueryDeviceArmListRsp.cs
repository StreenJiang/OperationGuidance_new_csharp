using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryDeviceArmListRsp: ControlResponse {
        public List<DeviceArmDTO> DeviceArmDTOs {
            get; set;
        }
    }
}
