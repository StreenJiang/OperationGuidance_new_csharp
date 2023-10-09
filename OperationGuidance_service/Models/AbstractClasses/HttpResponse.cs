using OperationGuidance_service.Constants;

namespace OperationGuidance_service.Models.AbstractClasses {
    public class HttpResponse {
        public HttpResponseCode RsponseCode {
            get; set; 
        } = HttpResponseCode.OK;

        public string RsponseMessage {
            get; set;
        } = string.Empty;
    }
}
