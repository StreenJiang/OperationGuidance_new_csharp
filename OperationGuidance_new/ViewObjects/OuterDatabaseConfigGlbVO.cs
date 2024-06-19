
using OperationGuidance_new.ViewObjects.AbstractClasses;
using OperationGuidance_new.Attributes;
using OperationGuidance_service.Configurations;

namespace OperationGuidance_new.ViewObjects {
    public class OuterDatabaseConfigGlbVO: AVOBase {
        [GridColumn("IP/HOST地址")]
        public string? host { get; set; }
        [GridColumn("端口号")]
        public int? port { get; set; }
        [GridColumn("数据库名")]
        public string? database_name { get; set; }
        [GridColumn("数据库类型")]
        public string? str_database_type { get; set; }
        private int? _database_type { get; set; }
        public int? database_type {
            get => _database_type;
            set {
                _database_type = value;
                if (value != null) {
                    if (value == (int) DBTypes.MYSQL) {
                        str_database_type = DBTypes.MYSQL + "";
                    } else if (value == (int) DBTypes.SQLITE) {
                        str_database_type = DBTypes.SQLITE + "";
                    } else if (value == (int) DBTypes.SQLSERVER) {
                        str_database_type = DBTypes.SQLSERVER + "";
                    }
                }
            }
        }
        [GridColumn("用户名")]
        public string? username { get; set; }
        // [GridColumn("密码")]
        // public string? password { get; set; }
        [GridColumn("工位号")]
        public string? workstation_name { get; set; }
    }
}
