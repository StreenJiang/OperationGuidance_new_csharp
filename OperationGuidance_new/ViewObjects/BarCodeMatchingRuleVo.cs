using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class BarCodeMatchingRuleVO: AVOBase {
        [GridColumn("长度")]
        public int? length { get; set; }
        [GridColumn("结束符")]
        public string? end_char { get; set; }
        [GridColumn("关键位")]
        public string? key_position { get; set; }
        [GridColumn("关键字符")]
        public string? key_char { get; set; }
        [GridColumn("条码类型")]
        public string? str_type { get; set; }
        private int? _type;
        public int? type { 
            get => _type; 
            set {
                _type = value;
                if (value != null) {
                    str_type = BarCodeTypes.GetNameById(value.Value);
                }
            }
        }
        [GridColumn("匹配任务")]
        public string? mission_name { get; set; }
        public int? mission_id { get; set; }
    }
}
