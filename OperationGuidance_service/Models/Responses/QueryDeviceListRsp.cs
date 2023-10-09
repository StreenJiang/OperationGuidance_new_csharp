using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses
{
    public class QueryDeviceListRsp: HttpResponse {
        public List<DeviceDTO> DeviceDTOs {
            get; set;
        }
    }
}
