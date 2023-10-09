using OperationGuidance_service.Constants;

namespace OperationGuidance_service.Utils {
    public class ConnectionUtils {
        public static ConnectionStatus CheckConnection(string ip, int port) {
            return ConnectionStatus.CONNECTED;
        }
    }
}
