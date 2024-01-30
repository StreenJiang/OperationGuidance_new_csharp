namespace OperationGuidance_new.Constants {
    public class Command {
        private string MESSAGE_SEND { get; set; }

        public Command(string messageSend) {
            MESSAGE_SEND = messageSend;
        }

        public string GetMessage(params string[] parameters) => string.Format(MESSAGE_SEND, parameters);
    }
}
