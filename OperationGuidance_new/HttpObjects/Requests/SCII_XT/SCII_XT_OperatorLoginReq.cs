using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests.SCII_XT {
    public class SCII_XT_OperatorLoginReq: HttpRequestBase_SCII_XT {
        public string employeeNumber { get; set; }
        public string password { get; set; }

        public SCII_XT_OperatorLoginReq(string employeeNumber, string password) {
            this.employeeNumber = employeeNumber;
            this.password = password;
        }
    }
}
