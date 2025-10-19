using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class SCII_XT_SwitchRecipeReq: HttpRequest {
        public string? recipeCode { get; set; }
        public int plcIndex { get; set; } = -1;
    }
}
