using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests.SCII_XT {
    public class SCII_XT_BindAccessoryReq: HttpRequestBase_SCII_XT {
        public string productCode { get; set; } = string.Empty;
        public string vehicleCode { get; set; } = string.Empty;
        public string procedureCode { get; set; } = string.Empty;
        public string recipeCode { get; set; } = string.Empty;
        public string accessoryCode { get; set; } = string.Empty;
        public List<Accessory> accessorys { get; set; } = new();
        public int createBy { get; set; }
        public string employeeNumber { get; set; } = string.Empty;

        public class Accessory {
            public string accessoryCode { get; set; } = string.Empty;
            public string accessoryType { get; set; } = string.Empty;
            public string partNo { get; set; } = string.Empty;
            public int orderId { get; set; }
        }
    }
}
