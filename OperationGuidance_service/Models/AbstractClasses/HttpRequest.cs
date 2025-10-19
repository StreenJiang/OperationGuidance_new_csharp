using Newtonsoft.Json;

namespace OperationGuidance_service.Models.AbstractClasses {
    public abstract class HttpRequest {
        public string ToJson() {
            return JsonConvert.SerializeObject(this);
        }
    }
}
