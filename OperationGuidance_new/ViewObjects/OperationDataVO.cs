using OperationGuidance_new.Attributes;
using OperationGuidance_new.ViewObjects.AbstractClasses;

namespace OperationGuidance_new.ViewObjects {
    public class OperationDataVO: AVOBase {
        [GridColumn("pesetid")]
        public int? procedure_set { get; set; }
        [GridColumn("拧紧状态")]
        public string? tightened_status { get; set; }
        [GridColumn("扭矩状态")]
        public string? torque_status { get; set; }
        [GridColumn("角度状态")]
        public string? angle_status { get; set; }
        [GridColumn("扭矩")]
        public float? torque { get; set; }
        [GridColumn("扭矩上限")]
        public float? torque_max { get; set; }
        [GridColumn("角度")]
        public float? angle { get; set; }
        [GridColumn("角度上线")]
        public float? angle_max { get; set; }
        [GridColumn("目标角度")]
        public float? angle_target { get; set; }
        [GridColumn("角度下限")]
        public float? angle_min { get; set; }
        [GridColumn("目标批次")]
        public int? batch_current { get; set; }
        [GridColumn("总批次")]
        public int? batch_sum { get; set; }
        [GridColumn("批次状态")]
        public string? batch_status { get; set; }
        public DateTime? filter_create_time_min { get; set; }
        public DateTime? filter_create_time_max { get; set; }
    }
}
