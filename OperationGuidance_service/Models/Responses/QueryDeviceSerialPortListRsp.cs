using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryDeviceSerialPortListRsp: ControlResponse {
        public List<DeviceSerialPortDTO> DeviceSerialPortDTOs {
            get; set;
        }
    }
}
