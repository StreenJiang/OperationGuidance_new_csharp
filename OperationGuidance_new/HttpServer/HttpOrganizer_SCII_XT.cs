using OperationGuidance_new.HttpObjects;

namespace OperationGuidance_new.HttpServer {
    public class HttpOrganizer_SCII_XT: HttpOrganizer {
        public HttpOrganizer_SCII_XT(int? port = 5000) : base(port) { }

        protected override void AddControllers() {
            if (_restfulHttpServer != null) {
                // Add controllers for SCII_XT
                SciiXtController.AddControllers(_restfulHttpServer);
            }
        }
    }
}
