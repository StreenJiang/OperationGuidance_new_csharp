using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public class Register {
        // Every register in modbus is 2 bytes long (16 bits)
        public static readonly int Bytes = 2;
        public static readonly int Bits = Bytes * 8;

        private byte[] _bytevalues;
        private int[] _values;

        public int[] Values => _values;
        public byte[] ByteValues {
            get => _bytevalues;
            set {
                _bytevalues = value;
                _values = MainUtils.ToIntsByHexString(MainUtils.ToHexString(_bytevalues));
            }
        }

        public Register() {
            _bytevalues = new byte[Bytes];
            _values = new int[Bits];
        }

        public int ToInt() => Convert.ToInt32(ToBinaryString(), 2);
        public string ToBinaryString() => MainUtils.ToBinaryString(_bytevalues);
        public string ToHexString() => MainUtils.ToHexString(_bytevalues);
    }
}
