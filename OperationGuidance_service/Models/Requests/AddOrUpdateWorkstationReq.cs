using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateWorkstationReq: HttpRequest {
        public WorkstationDTO WorkstationDTO { get; set; }

        public AddOrUpdateWorkstationReq(WorkstationDTO workstationDTO) {
            WorkstationDTO = workstationDTO;
        }
    }
}
