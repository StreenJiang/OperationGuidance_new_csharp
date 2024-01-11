using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;

namespace OperationGuidance_new.ViewObjects {
    public class BrandVO: AVOBase {
        [GridColumn("品牌名称")]
        public string? name { get; set; }
        [GridColumn("品牌描述")]
        public string? description { get; set; }
    }
}
