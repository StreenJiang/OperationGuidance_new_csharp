using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests {
    public class HttpRequestGetMatCode: HttpRequestBase_WHYC {
        public string QrCode;

        public HttpRequestGetMatCode(string qrCode) {
            QrCode = qrCode;
        }
    }
}
