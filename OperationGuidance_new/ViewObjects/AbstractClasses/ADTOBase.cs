using OperationGuidance_new.Attributes;

namespace OperationGuidance_new.ViewObjects.AbstractClasses {
    public abstract class AVOBase {
        public int? id { get; set; }
        [GridColumn("创建人")]
        public string? creator { get; set; }
        [GridColumn("最后修改人")]
        public string? modifier { get; set; }
        [GridColumn("创建时间")]
        public DateTime? create_time { get; set; }
        [GridColumn("最后修改时间")]
        public DateTime? modify_time { get; set; }
    }
}
