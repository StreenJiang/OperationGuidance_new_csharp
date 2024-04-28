using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public class MessageSegment {
        private int _startIndex;
        private int _length; // Count by byte
        private byte[]? _messageHexBytes;
        private Action<int, int>? _messageChanged;

        public int StartIndex { get => _startIndex; set => _startIndex = value; }
        public int Length { get => _length; set => _length = value; }

        public byte[]? MessageHexBytes {
            get => _messageHexBytes;
            set {
                // If length is equal to 0, means we don't know the length of it beforehand
                if (_length > 0) {
                    if (value != null && value.Length != _length) {
                        string errorMsg = $"Length of value[{value}:{value.Length}] is not equal to SEGMENT length[{_length}].";
                        throw new InvalidDataException(errorMsg);
                    }
                } else {
                    if (value != null) {
                        _length = value.Length;
                    }
                }
                if (_messageChanged != null) {
                    _messageChanged(_startIndex, _length);
                }
                _messageHexBytes = value;
            }
        }
        public event Action<int, int> OnMessageChanged { add => _messageChanged += value; remove => _messageChanged -= value; }

        public List<byte>? MessageHexByteList => _messageHexBytes == null ? null : _messageHexBytes.ToList();
        public string? MessageHexString => _messageHexBytes == null ? null : MainUtils.ToHexString(_messageHexBytes);
        public int[]? MessageBinaries => MessageHexString == null ? null : MainUtils.ToIntsByHexString(MessageHexString);
        public List<int>? MessageBinaryList => MessageBinaries == null ? null : MessageBinaries.ToList();

        public MessageSegment(int startIndex, int length) {
            _startIndex = startIndex;
            _length = length;
        }
    }
}
