using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;

namespace OperationGuidance_new.ViewObjects {
    public class UserAccountInfoVO: AVOBase {
        [GridColumn("员工ID")]
        public int? staff_id { get; set; }
        [GridColumn("姓名")]
        public string? name { get; set; }
        [GridColumn("角色")]
        public string? position { get; set; }
        [GridColumn("账户名")]
        public string? account { get; set; }
        [GridColumn("密码")]
        public string? password { get; set; }
    }
}
