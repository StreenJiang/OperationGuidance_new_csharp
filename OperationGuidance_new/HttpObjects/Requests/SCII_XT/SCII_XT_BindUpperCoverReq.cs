using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests.SCII_XT {
    public class SCII_XT_BindUpperCoverReq: HttpRequestBase_SCII_XT {
        public string productCode { get; set; } = string.Empty;
        public string upperCoverCode { get; set; } = string.Empty;
        public string employeeNumber { get; set; } = string.Empty;
    }
}
