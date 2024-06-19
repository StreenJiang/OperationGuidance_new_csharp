using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryOuterDatabaseConfigGlbListReq: HttpRequest {
        public int MacsId { get; set; }
        public QueryOuterDatabaseConfigGlbListReq(int macsId) => MacsId = macsId;
    }
}
