using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class DeleteUserAccountInfoByIdsReq: HttpRequest {
        public List<int> Ids { get; set; }

        public DeleteUserAccountInfoByIdsReq(List<int> ids) {
            Ids = ids;
        }
    }
}
