using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;

namespace OperationGuidance_new.ViewObjects {
    public class BrandVO: AVOBase {
        [GridColumn("品牌名称")]
        public string? name { get; set; }
        [GridColumn("品牌简称")]
        public string? short_name { get; set; }
        [GridColumn("品牌英文名称")]
        public string? english_name { get; set; }
        [GridColumn("品牌英文简称")]
        public string? english_short_name { get; set; }
        [GridColumn("品牌描述")]
        public string? description { get; set; }
    }
}
