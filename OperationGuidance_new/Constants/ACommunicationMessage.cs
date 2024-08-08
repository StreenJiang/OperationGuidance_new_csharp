using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Constants {
    public abstract class AModBusMessage {
        private int _lengthOfSymbols;
        private string? _sourceDataStr;
        private byte[]? _sourceData;
        protected List<byte> _messageData = new();

        private MessageSegment _transactionSymbol = new(0, 2);
        private MessageSegment _protocolSymbol = new(2, 2);
        private MessageSegment _lengthSymbol = new(4, 2);
        private MessageSegment _unitSymbol = new(6, 1);
        private MessageSegment _functionSymbol = new(7, 1);

        private MessageSegment? _registerStartHigt;
        private MessageSegment? _registerStartLow;
        private MessageSegment? _registerNumHigh;
        private MessageSegment? _registerNumLow;

        private MessageSegment? _dataLength;
        private MessageSegment? _data;

        public int LengthOfSymbols { get => _lengthOfSymbols; set => _lengthOfSymbols = value; }
        public string? SourceDataStr {
            set {
                _sourceDataStr = value;
                if (value != null) {
                    SourceData = MainUtils.ToBytes(value);
                }
            }
        }
        public byte[]? SourceData {
            set {
                _sourceData = value;
                if (value != null) {
                    SetValueToSegment(_transactionSymbol);
                    SetValueToSegment(_protocolSymbol);
                    SetValueToSegment(_lengthSymbol);
                    SetValueToSegment(_unitSymbol);
                    SetValueToSegment(_functionSymbol);
                    SetValueToSegment(_registerStartHigt);
                    SetValueToSegment(_registerStartLow);
                    SetValueToSegment(_registerNumHigh);
                    SetValueToSegment(_registerNumLow);
                    SetValueToSegment(_dataLength);
                    SetValueToSegment(_data);
                }
            }
        }
        public virtual byte[] MessageData {
            get {
                if (_sourceData != null) {
                    return _sourceData;
                }
                _messageData = SummaryData();
                return _messageData.ToArray();
            }
        }
        public string MessageDataString => MessageData == null ? "" : MainUtils.ToHexString(MessageData);

        public MessageSegment TransactionSymbol => _transactionSymbol;
        public MessageSegment ProtocolSymbol => _protocolSymbol;
        public MessageSegment LengthSymbol => _lengthSymbol;
        public MessageSegment UnitSymbol => _unitSymbol;
        public MessageSegment FunctionSymbol => _functionSymbol;

        public MessageSegment? RegisterStart { get => _registerStartHigt; protected set => _registerStartHigt = value; }
        public MessageSegment? RegisterNum { get => _registerNumLow; protected set => _registerNumLow = value; }

        public MessageSegment? DataLength { get => _dataLength; protected set => _dataLength = value; }
        public MessageSegment? Data { get => _data; protected set => _data = value; }

        public AModBusMessage(byte[]? sourceData = null) {
            _sourceData = sourceData;

            InitializeOtherSegments();

            _protocolSymbol.MessageHexBytes = new byte[] { 0x00, 0x00 };
            _unitSymbol.MessageHexBytes = new byte[] { 0xff };

            _lengthOfSymbols = _transactionSymbol.Length + _protocolSymbol.Length + _lengthSymbol.Length
                + _unitSymbol.Length + _functionSymbol.Length;
            if (_dataLength != null) {
                _lengthOfSymbols += _dataLength.Length;
            }
        }
        protected abstract void InitializeOtherSegments();

        protected List<byte> SummaryData() {
            List<byte> messageBytes = new();
            AddBytes(messageBytes, _transactionSymbol);
            AddBytes(messageBytes, _protocolSymbol);
            AddBytes(messageBytes, _lengthSymbol);
            AddBytes(messageBytes, _unitSymbol);
            AddBytes(messageBytes, _functionSymbol);
            AddBytes(messageBytes, _registerStartHigt);
            AddBytes(messageBytes, _registerStartLow);
            AddBytes(messageBytes, _registerNumHigh);
            AddBytes(messageBytes, _registerNumLow);
            AddBytes(messageBytes, _dataLength);
            AddBytes(messageBytes, _data);
            return messageBytes;
        }
        protected void AddBytes(List<byte> data, MessageSegment? segment) {
            if (segment != null) {
                if (segment.MessageHexBytes == null) {
                    string errorMsg = $"MessageHexBytes can not be null";
                    throw new InvalidCastException(errorMsg);
                }
                data.AddRange(segment.MessageHexBytes);
            }
        }
        protected void SetValueToSegment(MessageSegment? segment) {
            if (segment != null && _sourceData != null) {
                if (segment.Length == 0 && segment.StartIndex < _sourceData.Length - 1) {
                    // If length is equal to 0, means we don't know the length of it beforehand
                    segment.MessageHexBytes = _sourceData.Skip(segment.StartIndex).ToArray();
                } else if (segment.StartIndex + segment.Length < _sourceData.Length - 1) {
                    // Other segments that specify length will only take content of the certain length
                    segment.MessageHexBytes = _sourceData.Skip(segment.StartIndex).Take(segment.Length).ToArray();
                }
            }
        }
    }
}
