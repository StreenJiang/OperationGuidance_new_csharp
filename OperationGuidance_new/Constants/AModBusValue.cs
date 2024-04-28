using System.Text;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public abstract class AModBusValue {
        public int IndexInServer { get; set; }

        // Initialize to 2 bytes as length of each register is 2 bytes
        private byte[] _bytesValue = new byte[0];
        public virtual byte[] BytesValue {
            get => _bytesValue;
            set {
                _bytesValue = value;
                if (_bytesValue.Length > 0) {
                    _hexStringValue = MainUtils.ToHexString(_bytesValue);
                } else {
                    _bytesValue = new byte[0];
                    _hexStringValue = "";
                }
            }
        }

        // Initialize to 2 bytes as length of each register is 2 bytes
        private string _hexStringValue = "";
        public virtual string HexStringValue {
            get => _hexStringValue;
            set {
                _hexStringValue = value;
                if (!string.IsNullOrEmpty(value)) {
                    _bytesValue = MainUtils.ToBytes(_hexStringValue);
                } else {
                    _hexStringValue = "";
                    _bytesValue = new byte[0];
                }
            }
        }
        public string ASCIIStringValue
            => string.IsNullOrEmpty(_hexStringValue) ? ""
                : Encoding.ASCII.GetString(MainUtils.ToBytes(_hexStringValue).Where(b => b != 0x00 && b < 185).ToArray()).Replace(@"[^\w\.@-]", "");

        // public string ASCIIStringValue => string.IsNullOrEmpty(_hexStringValue) ? "" : Encoding.ASCII.GetString(MainUtils.ToBytes(_hexStringValue));

        public AModBusValue(int indexInServer) {
            IndexInServer = indexInServer;
        }
    }
}
