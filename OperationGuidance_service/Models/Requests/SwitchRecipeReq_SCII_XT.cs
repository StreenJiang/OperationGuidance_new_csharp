using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class SwitchRecipeReq_SCII_XT: HttpRequest {
        public string? recipeCode { get; set; }
        public int plcIndex { get; set; } = -1;
    }
}
