using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class AddOrUpdateMatCodeMapWhycRsp: HttpResponse {
        public MatCodeMapWhycDTO MatCodeMapWhycDTO { get; set; } = new();
    }
}
