using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryDeviceCommunicationListReq: HttpRequest {
        public bool ForTask { get; set; } = false;
        public int MacsId { get; set; }
        public QueryDeviceCommunicationListReq(int macsId) => MacsId = macsId;
    }
}
