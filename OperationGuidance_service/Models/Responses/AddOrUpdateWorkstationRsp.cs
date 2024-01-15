using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateWorkstationRsp: HttpResponse {
        public WorkstationDTO WorkstationDTO { get; set;} = new();
    }
}
