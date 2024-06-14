using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindOperationDataByIdReq : HttpRequest {
        public int Id { get; set; }

        public FindOperationDataByIdReq(int id) {
            Id = id;
        }
    }
}
