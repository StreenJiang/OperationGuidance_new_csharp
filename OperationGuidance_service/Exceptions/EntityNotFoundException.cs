using System.Runtime.Serialization;

namespace OperationGuidance_service.Exceptions {
    public class EntityNotFoundException: Exception {
        public EntityNotFoundException() {
        }

        public EntityNotFoundException(string? message) : base(message) {
        }

        public EntityNotFoundException(string? message, Exception? innerException) : base(message, innerException) {
        }

        protected EntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
