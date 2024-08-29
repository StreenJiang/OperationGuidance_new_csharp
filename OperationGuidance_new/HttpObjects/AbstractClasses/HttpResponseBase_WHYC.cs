using OperationGuidance_new.Constants;

namespace OperationGuidance_new.HttpObjects.AbstractClasses {
    public abstract class HttpResponseBase_WHYC {
        public HttpStatus_WHYC unStatus { get; set; } = HttpStatus_WHYC.FAILURE;
        public string? ucMsg { get; set; }
    }
}
