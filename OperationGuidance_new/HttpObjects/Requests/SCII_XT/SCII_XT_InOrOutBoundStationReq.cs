using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests.SCII_XT {
    public class SCII_XT_InOrOutBoundStationReq: HttpRequestBase_SCII_XT {
        public string productCode { get; set; } = string.Empty;
        public string vehicleCode { get; set; } = string.Empty;
        public int passType { get; set; }
        public string recipeCode { get; set; } = string.Empty;
        public string procedureCode { get; set; } = string.Empty;
        public string equipmentCode { get; set; } = string.Empty;
        public string batchNo { get; set; } = string.Empty;
        public bool result { get; set; }
        public string ngReasonDetail { get; set; } = string.Empty;
    }
}
