using OperationGuidance_service.Configurations;

namespace OperationGuidance_service.Utils {
    public class SystemInitializer {
        public static void Initialize() {
            // Initialize dependencies
            DependencyInjector.Initialize();
            // Initialze UserAccountInfo
        }
    }
}
