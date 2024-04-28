namespace OperationGuidance_new.Constants {
    public class ModBusServer_YF: ModBusServerBase {
        private ModBusString _kpIdentify;
        private ModBusString _kpTask;
        private ModBusBool _eccRequest;
        private ModBusBool _kpRelease;
        private ModBusBool _eccStop;
        private ModBusBool _eccFinish;
        private ModBusBool _kpAck;
        private ModBusBool _kpInitial;
        private ModBusBool _eccKeepAlive;

        public ModBusString KpIdentify { get => _kpIdentify; set => _kpIdentify = value; }
        public ModBusString KpTask { get => _kpTask; set => _kpTask = value; }
        public ModBusBool EccRequest { get => _eccRequest; set => _eccRequest = value; }
        public ModBusBool KpRelease { get => _kpRelease; set => _kpRelease = value; }
        public ModBusBool EccStop { get => _eccStop; set => _eccStop = value; }
        public ModBusBool EccFinish { get => _eccFinish; set => _eccFinish = value; }
        public ModBusBool KpAck { get => _kpAck; set => _kpAck = value; }
        public ModBusBool KpInitial { get => _kpInitial; set => _kpInitial = value; }
        public ModBusBool EccKeepAlive { get => _eccKeepAlive; set => _eccKeepAlive = value; }

        public ModBusServer_YF(int startAddress, int length) : base(startAddress, length) {
            // String values
            _kpIdentify = new(startAddress + 9, 50);
            _kpTask = new(startAddress + 1, 2);
            // Bool values
            _eccRequest = new(startAddress, 10);
            _kpRelease = new(startAddress, 1);
            _eccStop = new(startAddress, 4);
            _eccFinish = new(startAddress, 9);
            _kpAck = new(startAddress, 2);
            _kpInitial = new(startAddress, 3);
            _eccKeepAlive = new(startAddress, 8);
        }

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
            UpdateValue(_kpIdentify);
            UpdateValue(_kpTask);
            UpdateValue(_eccRequest);
            UpdateValue(_kpRelease);
            UpdateValue(_eccStop);
            UpdateValue(_eccFinish);
            UpdateValue(_kpAck);
            UpdateValue(_kpInitial);
            UpdateValue(_eccKeepAlive);
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
