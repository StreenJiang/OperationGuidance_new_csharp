using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryDeviceSerialPortListRsp: HttpResponse {
        public List<DeviceSerialPortDTO> DeviceSerialPortDTOs {
            get; set;
        }
    }
}
