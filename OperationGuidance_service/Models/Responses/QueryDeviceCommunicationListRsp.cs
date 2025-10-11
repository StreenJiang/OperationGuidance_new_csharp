using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryDeviceCommunicationListRsp: ControlResponse {
        public List<DeviceCommunicationDTO> DeviceCommunicationDTOs {
            get; set;
        }
    }
}
