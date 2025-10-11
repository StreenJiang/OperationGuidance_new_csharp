using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryWorkstationListReq: ControlRequest {
        public int MacsId { get; set; }
        public QueryWorkstationListReq(int macsId) => MacsId = macsId;
    }
}
