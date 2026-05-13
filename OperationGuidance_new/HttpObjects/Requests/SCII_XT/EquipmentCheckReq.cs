using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests.SCII_XT {
    public class EquipmentCheckReq : HttpRequestBase_SCII_XT {
        public List<EquipmentCheckInfo> equipmentCheckInfos { get; set; } = new();
        public string employeeNumber { get; set; } = string.Empty;
        public string equipmentCode { get; set; } = string.Empty;

        public class EquipmentCheckInfo {
            public List<Attribute> attributeList { get; set; } = new();
        }

        public class Attribute {
            public string attributeName { get; set; } = string.Empty;
            public string attributeCode { get; set; } = string.Empty;
            public string attributeUnit { get; set; } = string.Empty;
            public int attributeType { get; set; }
            public int orderId { get; set; }
            public string value { get; set; } = string.Empty;
        }
    }
}
