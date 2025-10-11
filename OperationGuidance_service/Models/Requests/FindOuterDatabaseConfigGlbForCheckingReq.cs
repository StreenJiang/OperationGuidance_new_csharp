using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindOuterDatabaseConfigGlbForCheckingReq: ControlRequest {
        public int Id { get; set; }
        public string host { get; set; }
        public int port { get; set; }
        public string database_name { get; set; }
        public int database_type { get; set; }
        public string workstation_name { get; set; }

        public FindOuterDatabaseConfigGlbForCheckingReq(int id, string host, int port, string database_name, int database_type, string workstation_name) {
            Id = id;
            this.host = host;
            this.port = port;
            this.database_name = database_name;
            this.database_type = database_type;
            this.workstation_name = workstation_name;
        }
    }
}
