using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public partial class QueryProductMissionsReq: ControlRequest {
        public int MacsId { get; set; }
        public Roles? Role { get; set; }

        public QueryProductMissionsReq() { }
        public QueryProductMissionsReq(int macsId) => MacsId = macsId;
    }
}
