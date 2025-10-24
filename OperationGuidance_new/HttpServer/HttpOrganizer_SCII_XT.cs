using OperationGuidance_new.HttpObjects;

namespace OperationGuidance_new.HttpServer {
    public class HttpOrganizer_SCII_XT: HttpOrganizer {
        protected override void AddControllers() {
            if (_restfulHttpServer != null) {
                // Add controllers for SCII_XT
                SciiXtController.AddControllers(_restfulHttpServer);
            }
        }
    }
}
