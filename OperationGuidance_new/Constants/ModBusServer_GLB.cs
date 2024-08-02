namespace OperationGuidance_new.Constants {
    public class ModBusServer_GLB: ModBusServerBase {
        private ModBusString _barCdoe;

        public ModBusServer_GLB(int startAddress, int length) : base(startAddress, length) {
            // String values
            _barCdoe = new(startAddress, length);
        }

        public ModBusString BarCdoe { get => _barCdoe; set => _barCdoe = value; }

        public override void LoadData(byte[] dataFromServer) {
            int index = 0;
            foreach (Register register in Registers) {
                byte[] bytes = dataFromServer.Skip(index).Take(Register.Bytes).ToArray();
                register.ByteValues = bytes;
                index += Register.Bytes;
            }
            UpdateValue();
        }

        public void UpdateValue() {
            UpdateValue(_barCdoe);
        }
        public void UpdateValue(AModBusValue modBusValue) {
            int startIndex = modBusValue.IndexInServer - StartAddress;
            if (modBusValue is ModBusBool boolValue) {
                boolValue.HexStringValue = Registers[startIndex].ToHexString();
            } else if (modBusValue is ModBusString stringValue) {
                stringValue.HexStringValue = "";
                int count = 0;
                for (int i = startIndex; i < startIndex + stringValue.ByteNum / Register.Bytes; i++) {
                    string value = Registers[i].ToHexString();
                    if (string.IsNullOrEmpty(value) || value == "0000") {
                        break;
                    }
                    stringValue.HexStringValue += Registers[i].ToHexString();
                    count++;
                }
            }
        }
    }
}
