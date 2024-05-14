using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("sql_execute_record")]
    public class SqlExecuteRecord: AEntityBase {
        public string? file_name { get; set; }
    }
}
