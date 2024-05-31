using OperationGuidance_new.Attributes;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.ViewObjects.AbstractClasses {
    public abstract class AVOBase {
        public virtual int? id { get; set; }
        [GridColumn("创建人")]
        public string? creator { get; set; }
        [GridColumn("最后修改人")]
        public string? modifier { get; set; }
        [GridColumn("创建时间")]
        public virtual string? string_create_time { get; set; }
        private DateTime? _create_time;
        public DateTime? create_time {
            get => _create_time;
            set {
                _create_time = value;
                if (value != null) {
                    string_create_time = value.Value.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS);
                }
            }
        }
        [GridColumn("最后修改时间")]
        public virtual string? string_modify_time { get; set; }
        private DateTime? _modify_time;
        public DateTime? modify_time {
            get => _modify_time;
            set {
                _modify_time = value;
                if (value != null) {
                    string_modify_time = value.Value.ToString(MainUtils.DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS);
                }
            }
        }
    }
}
