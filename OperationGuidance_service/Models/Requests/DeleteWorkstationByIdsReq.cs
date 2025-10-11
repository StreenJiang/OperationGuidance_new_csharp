using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteWorkstationByIdsReq: ControlRequest {
        public List<int> Ids { get; set; }

        public DeleteWorkstationByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
