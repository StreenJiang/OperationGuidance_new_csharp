using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryMatCodeMapWhycListReq: ControlRequest {
        public int MacsId { get; set; }
        public QueryMatCodeMapWhycListReq(int macsId) => MacsId = macsId;
    }
}
