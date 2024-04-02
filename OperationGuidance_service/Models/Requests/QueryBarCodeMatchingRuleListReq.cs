using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryBarCodeMatchingRuleListReq: HttpRequest {
        public int? MissionId { get; set; }
        public int MacsId { get; set; }
        public QueryBarCodeMatchingRuleListReq(int macsId) => MacsId = macsId;
    }
}
