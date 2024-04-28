using System.Text;

namespace OperationGuidance_new.Constants {
    public class ModBusString: AModBusValue {
        private string _stringValue = "";
        public string StringValue {
            get => _stringValue;
        }
        public void SetStringValue(string value, int length) {
            _stringValue = value;
            List<byte> bytes = Encoding.ASCII.GetBytes(_stringValue).ToList();
            if (bytes.Count < length) {
                bytes.AddRange(new byte[length - bytes.Count]);
            }
            BytesValue = bytes.ToArray();
        }

        public int ByteNum { get; set; }
        public ModBusString(int indexInServer, int byteNum) : base(indexInServer) {
            ByteNum = byteNum;
        }
    }
}
