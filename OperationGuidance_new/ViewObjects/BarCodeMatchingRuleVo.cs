using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class BarCodeMatchingRuleVO: AVOBase {
        [GridColumn("ID")]
        public override int? id { get; set; }
        [GridColumn("长度")]
        public virtual int? length { get; set; }
        [GridColumn("结束符")]
        public virtual string? end_char { get; set; }
        [GridColumn("关键位")]
        public virtual string? key_position { get; set; }
        [GridColumn("关键字符")]
        public virtual string? key_char { get; set; }
        [GridColumn("条码类型")]
        public virtual string? str_type { get; set; }
        protected int? _type;
        public virtual int? type {
            get => _type;
            set {
                _type = value;
                if (value != null) {
                    str_type = BarCodeTypes.GetNameById(value.Value);
                }
            }
        }
        [GridColumn("匹配任务")]
        public virtual string? mission_name { get; set; }
        public virtual int? mission_id { get; set; }
        public virtual int macs_id { get; set; }
    }
}
