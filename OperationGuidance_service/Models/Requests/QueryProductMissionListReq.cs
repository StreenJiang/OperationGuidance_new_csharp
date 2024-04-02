using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public partial class QueryProductMissionListReq: HttpRequest {
        public int MacsId { get; set; }
        public bool? IsEditing { get; set; }

        public QueryProductMissionListReq(int macsId) => MacsId = macsId;
    }
}
