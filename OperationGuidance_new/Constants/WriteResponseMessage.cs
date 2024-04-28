namespace OperationGuidance_new.Constants {
    public class WriteResponseMessage: ACommunicationMessage {
        protected override void InitializeOtherSegments() {
            RegisterStart = new(8, 2);
            RegisterNum = new(10, 2);
        }
    }
}
