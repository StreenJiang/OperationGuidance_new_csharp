namespace OperationGuidance_new.Constants {
    public abstract class ModBusServerBase {
        private int _startAddress;
        private int _length;
        private Register[] _registers;

        public int StartAddress { get => _startAddress; set => _startAddress = value; }
        public int Length { get => _length; set => _length = value; }
        protected Register[] Registers { get => _registers; set => _registers = value; }

        public ModBusServerBase(int startAddress, int length) {
            _startAddress = startAddress;
            _length = length;
            _registers = new Register[length];
            for (int i = 0; i < Registers.Length; i++) {
                Registers[i] = new();
            }
        }

        public abstract void LoadData(byte[] dataFromServer);

        public byte[] ResetBytes() {
            return new byte[_length];
        }
    }
}
