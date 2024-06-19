using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Responses {
    public class AddDataToOuterDatabaseGlbRsp: HttpResponse {
        public int Rows { get; set; }

        public AddDataToOuterDatabaseGlbRsp(int rows) {
            Rows = rows;
        }
    }
}
