using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;

namespace OperationGuidance_new.ViewObjects {
    public class MatCodeMapWhycVO: AVOBase {
        [GridColumn("MatCode")]
        public string? mat_code { get; set; }
        [GridColumn("程序号")]
        public int? parameter_set { get; set; }
        public int macs_id { get; set; }
    }
}
