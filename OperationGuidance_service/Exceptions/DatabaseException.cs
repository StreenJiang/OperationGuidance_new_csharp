using System.Runtime.Serialization;

namespace OperationGuidance_service.Exceptions {
    public class DatabaseException: Exception {
        public DatabaseException() {
        }

        public DatabaseException(string? message) : base(message) {
        }

        public DatabaseException(string? message, Exception? innerException) : base(message, innerException) {
        }

        protected DatabaseException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
