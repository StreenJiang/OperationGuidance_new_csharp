using CustomLibrary.Buttons;
using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class WorkstationVO: AVOBase {
        [GridColumn("站点名称")]
        public string? name { get; set; }
        [GridColumn("工具名称")]
        public string? tool_name { get; set; }
        [GridColumn("工具型号")]
        public string? tool_device_model_name { get; set; }
        [GridColumn("工具IP")]
        public string? tool_ip { get; set; }
        [GridColumn("工具端口")]
        public int? tool_port { get; set; }
        [GridColumn("力臂名称")]
        public string? arm_name { get; set; }
        [GridColumn("力臂型号")]
        public int? arm_device_type_id { get; set; }
        [GridColumn("力臂IP")]
        public string? arm_ip { get; set; }
        [GridColumn("力臂端口")]
        public int? arm_port { get; set; }
        [GridColumn("是否启用", typeof(ToggleButton))]
        public bool? bool_enabled { get; set; }
        // This is to get the value from database, will change it to bool above
        public int? enabled { 
            get {
                if (bool_enabled != null && bool_enabled.Value) {
                    return (int) YesOrNo.YES;
                } else {
                    return (int) YesOrNo.NO;
                }
            } 
            set {
                if (value != null && value.Value == (int) YesOrNo.YES) {
                    bool_enabled = true;
                } else {
                    bool_enabled = false;
                }
            }
        }
    }
}
