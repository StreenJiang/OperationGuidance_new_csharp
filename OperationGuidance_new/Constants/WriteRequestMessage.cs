using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public class WriteRequestMessage: AModBusMessage {
        protected override void InitializeOtherSegments() {
            RegisterStart = new(8, 2);
            RegisterNum = new(10, 2);

            // Needs to write something so instanciate them
            DataLength = new(8, 1);
            Data = new(9, 0);

            TransactionSymbol.MessageHexBytes = new byte[] { 0x00, 0x04 };
            FunctionSymbol.MessageHexBytes = new byte[] { 0x10 };

            RegisterStart.MessageHexBytes = new byte[] { 0x00, 0x00 };
            RegisterNum.MessageHexBytes = new byte[] { 0x00, 0x64 };
        }

        public void SetLength() {
            int length = 0;
            length += UnitSymbol.Length;
            length += FunctionSymbol.Length;
            length += RegisterStart.Length;
            length += RegisterNum.Length;
            length += DataLength.Length;
            length += Data.Length;

            LengthSymbol.MessageHexBytes = MainUtils.ToBytes(length);
        }
    }
}
