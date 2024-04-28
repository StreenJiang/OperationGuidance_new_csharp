namespace OperationGuidance_new.Constants {
    public class ReadResponseMessage: ACommunicationMessage {
        protected override void InitializeOtherSegments() {
            DataLength = new(8, 1);
            Data = new(9, 0);
        }
    }
}
