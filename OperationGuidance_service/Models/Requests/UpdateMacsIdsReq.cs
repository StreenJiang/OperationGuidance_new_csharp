using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class UpdateMacsIdsReq: ControlRequest {
        public int IdFrom { get; private set; }
        public int IdTo { get; private set; }

        public UpdateMacsIdsReq(int idFrom, int idTo) {
            IdFrom = idFrom;
            IdTo = idTo;
        }
    }
}
