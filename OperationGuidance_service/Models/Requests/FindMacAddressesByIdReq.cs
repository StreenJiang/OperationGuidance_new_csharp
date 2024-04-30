using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindMacAddressesByIdReq: HttpRequest {
        public int Id { get; set; }

        public FindMacAddressesByIdReq(int id) => Id = id;
    }
}
