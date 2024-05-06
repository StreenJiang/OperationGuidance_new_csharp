using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Responses {
    public class UpdateMacsIdsRsp: HttpResponse {
        public int UpdateRows {
            get; set;
        }
    }
}
