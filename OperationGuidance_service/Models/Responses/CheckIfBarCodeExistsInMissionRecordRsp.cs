using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Responses {
    public class CheckIfBarCodeExistsInMissionRecordRsp: HttpResponse {
        public bool Yes {
            get; set;
        }
    }
}
