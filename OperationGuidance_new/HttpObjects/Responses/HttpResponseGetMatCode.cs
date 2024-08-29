using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Response {
    public class HttpResponseGetMatCode: HttpResponseBase_WHYC {
        public UcData? ucData { get; set; }
    }

    public class UcData {
        public string? MatCode { get; set; }
    }
}
