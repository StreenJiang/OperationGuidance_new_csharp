using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteBarCodeMatchingRuleByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteBarCodeMatchingRuleByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
