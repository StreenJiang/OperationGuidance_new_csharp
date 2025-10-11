using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Requests {
    public class AddOrUpdateMatCodeMapWhycReq: ControlRequest {
        public MatCodeMapWhycDTO MatCodeMapWhycDTO { get; set; }

        public AddOrUpdateMatCodeMapWhycReq(MatCodeMapWhycDTO dto) {
            MatCodeMapWhycDTO = dto;
        }
    }
}
