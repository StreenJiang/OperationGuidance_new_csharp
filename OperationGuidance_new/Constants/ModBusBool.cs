using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public class ModBusBool: AModBusValue {
        public int ValuePosition { get; set; } = -1;

        public override byte[] BytesValue {
            get => base.BytesValue.Length == 0 ? new byte[] { 0x00, 0x00 } : base.BytesValue;
            set {
                byte high = value[0];
                byte low = value[1];

                // High and low are inverted for bool values
                base.BytesValue = new byte[] { low, high };
                // Set value to base.BytesValue will set to base.HexStringValue automatically
                int[] ints = MainUtils.ToIntsByHexString(base.HexStringValue).Reverse().ToArray();
                // If value position is equal to -1, means this is a temporary bool value
                if (ValuePosition > -1) {
                    _boolValue = ints[ValuePosition] == 1;
                }
            }
        }
        public override string HexStringValue {
            get => string.IsNullOrEmpty(base.HexStringValue) ? "0000" : base.HexStringValue;
            set {
                string high = value.Substring(0, 2);
                string low = value.Substring(2);

                // High and low are inverted for bool values
                base.HexStringValue = low + high;
                int[] ints = MainUtils.ToIntsByHexString(base.HexStringValue).Reverse().ToArray();
                _boolValue = ints[ValuePosition] == 1;
            }
        }
        private bool _boolValue = false;
        public bool BoolValue {
            get => _boolValue;
            set {
                if (string.IsNullOrEmpty(base.HexStringValue)) {
                    base.HexStringValue = "0000";
                }

                // Yes, here

                // High and low are already inverted inside
                char[] binaryChars = MainUtils.ToBinaryString(base.HexStringValue).Reverse().ToArray();
                binaryChars[ValuePosition] = value ? '1' : '0';
                string binaryString = new string(binaryChars.Reverse().ToArray());

                // Invert them back
                string high2 = binaryString.Substring(0, 8);
                string low2 = binaryString.Substring(8);

                // Set value to base.HexStringValue will set to base.BytesValue automatically, so don't need to set base.BytesValue again
                base.HexStringValue = MainUtils.ToHexString(low2 + high2);

                // It will set back to origin if put it above and don't know why
                _boolValue = value;
            }
        }

        public ModBusBool(int indexInServer, int valuePosition = -1, string? initValue = null) : base(indexInServer) {
            ValuePosition = valuePosition;
            if (initValue != null) {
                base.HexStringValue = initValue;
            }
        }

        public static ModBusBool operator |(ModBusBool b1, ModBusBool b2) {
            if (b1.IndexInServer != b2.IndexInServer) {
                string errorMsg = $"IndexInServer of two values must be the same.";
                throw new InvalidOperationException(errorMsg);
            }
            if (b1.ValuePosition == b2.ValuePosition) {
                string errorMsg = $"ValuePosition of two values can not be the same.";
                throw new InvalidOperationException(errorMsg);
            }
            if (b1.BytesValue.Length != b2.BytesValue.Length) {
                string errorMsg = $"Length of two values must be the same.";
                throw new InvalidOperationException(errorMsg);
            }
            if (b1.BytesValue.Length != Register.Bytes) {
                string errorMsg = $"Values that length is equal to {Register.Bytes} can use this operator '|' only.";
                throw new InvalidOperationException(errorMsg);
            }
            int[] v1 = MainUtils.ToIntsByHexString(b1.HexStringValue);
            int[] v2 = MainUtils.ToIntsByHexString(b2.HexStringValue);

            int[] temp = new int[v1.Length];
            for (int i = 0; i < v1.Length; i++) {
                temp[i] = v1[i] | v2[i];
            }
            int[] low = temp.Skip(0).Take(8).ToArray();
            int[] high = temp.Skip(8).Take(8).ToArray();
            List<int> result = new();
            // Use the right format 'high + low' here because when setting bytes value it will invert them inside
            result.AddRange(high);
            result.AddRange(low);

            ModBusBool newV = new(b1.IndexInServer);
            newV.BytesValue = MainUtils.ToBytesByBinaryString(string.Join("", result));
            return newV;
        }
    }
}
