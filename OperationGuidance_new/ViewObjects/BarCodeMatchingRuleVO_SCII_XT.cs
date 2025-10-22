
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class BarCodeMatchingRuleVO_SCII_XT: BarCodeMatchingRuleVO {
        [GridColumn("ID")]
        public override int? id { get; set; }
        [GridColumn("长度")]
        public override int? length { get; set; }
        [GridColumn("结束符")]
        public override string? end_char { get; set; }
        [GridColumn("关键位")]
        public override string? key_position { get; set; }
        [GridColumn("关键字符")]
        public override string? key_char { get; set; }
        [GridColumn("条码类型")]
        public override string? str_type { get; set; }
        protected new int? _type;
        public override int? type {
            get => _type;
            set {
                _type = value;
                if (value != null) {
                    str_type = BarCodeTypes.GetNameById(value.Value);
                }
            }
        }
        [GridColumn("物料名称")]
        public string? name { get; set; }
        [GridColumn("料号")]
        public string? part_no { get; set; }
        [GridColumn("匹配任务")]
        public override string? mission_name { get; set; }
        public override int? mission_id { get; set; }
        public override int macs_id { get; set; }
    }
}
