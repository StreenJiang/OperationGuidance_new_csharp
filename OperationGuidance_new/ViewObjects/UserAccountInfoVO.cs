using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.ViewObjects {
    public class UserAccountInfoVO: AVOBase {
        [GridColumn("员工ID")]
        public int? staff_id { get; set; }
        [GridColumn("姓名")]
        public string? name { get; set; }
        [GridColumn("职位")]
        public string? position { get; set; }
        [GridColumn("账户名")]
        public string? account { get; set; }
        public string? password { get; set; }
        [GridColumn("权限角色")]
        public string? str_role_type { get; set; }
        private int? _role_type;
        public int? role_type { 
            get => _role_type; 
            set {
                _role_type = value;
                if (value != null) {
                    str_role_type = Enum.GetName(typeof(Roles), value.Value);
                }
            }
        }
    }
}
