using OperationGuidance_service.Models.AbstractClasses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperationGuidance_service.Models {
    [Table("mac_addresses")]
    public class MacAddresses: AEntityBase {
        public string? macs { get; set; }
    }
}
