using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public class ReadRequestMessage: ACommunicationMessage {
        protected override void InitializeOtherSegments() {
            RegisterStart = new(8, 2);
            RegisterNum = new(10, 2);

            TransactionSymbol.MessageHexBytes = new byte[] { 0x00, 0x02 };
            FunctionSymbol.MessageHexBytes = new byte[] { 0x03 };

            RegisterStart.MessageHexBytes = new byte[] { 0x00, 0x00 };
            RegisterNum.MessageHexBytes = new byte[] { 0x00, 0x64 };

            int length = 0;
            length += UnitSymbol.Length;
            length += FunctionSymbol.Length;
            length += RegisterStart.Length;
            length += RegisterNum.Length;

            LengthSymbol.MessageHexBytes = MainUtils.ToBytes(length);
        }
    }
}
