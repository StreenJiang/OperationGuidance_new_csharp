using OperationGuidance_new.HttpObjects.AbstractClasses;

namespace OperationGuidance_new.HttpObjects.Requests.SCII_XT {
    public class SCII_XT_BindProductDataReq: HttpRequestBase_SCII_XT {
        public int bingType { get; set; }
        public string vehicleCode { get; set; } = string.Empty;
        public List<ProductInfo> productInfos { get; set; } = new();
        public string procedureCode { get; set; } = string.Empty;
        public string recipeCode { get; set; } = string.Empty;

        public class ProductInfo {
            public string productCode { get; set; } = string.Empty;
            public List<Attribute> attributeList { get; set; } = new();

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
}
