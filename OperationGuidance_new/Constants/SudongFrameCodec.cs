namespace OperationGuidance_new.Constants {
    /// <summary>
    /// 速动 X7 帧格式：55 AA &lt;len&gt; &lt;payload...&gt; &lt;crc2&gt; 0D 0A。
    /// 现场所有帧满足：总长度 = len + 4（len 为第 3 个字节）。
    /// TCP 是字节流，一次 Receive 可能包含多帧，也可能只有半帧；本类负责重组。
    /// </summary>
    public sealed class SudongFrameCodec {
        private const int MaxFrameBytes = 256;
        private const int MaxBufferBytes = 8 * 1024;
        private readonly List<byte> _buffer = new();

        /// <summary>因超过缓冲上限而被丢弃的字节总数（诊断用）。</summary>
        public long DroppedBytes { get; private set; }

        /// <summary>喂入一次 Receive 的原始字节，返回本次能拼出的完整帧（可能 0 个或多个）。</summary>
        public IReadOnlyList<byte[]> Feed(byte[] chunk) {
            var frames = new List<byte[]>();
            if (chunk != null && chunk.Length > 0) {
                _buffer.AddRange(chunk);
            }

            while (true) {
                // 1. 对齐帧头 55 AA
                int start = -1;
                for (int i = 0; i + 1 < _buffer.Count; i++) {
                    if (_buffer[i] == 0x55 && _buffer[i + 1] == 0xAA) {
                        start = i;
                        break;
                    }
                }
                if (start < 0) {
                    // 保留最后一个字节，它可能是下一帧 0x55 的后半截。
                    // 超过缓冲上限的垃圾数据不在此处裁剪，交给末尾的上限检查统一清空并计数。
                    if (_buffer.Count > 1 && _buffer.Count <= MaxBufferBytes) {
                        _buffer.RemoveRange(0, _buffer.Count - 1);
                    }
                    break;
                }
                if (start > 0) {
                    _buffer.RemoveRange(0, start);
                }
                if (_buffer.Count < 3) {
                    break;
                }

                // 2. 长度前缀：总长 = len + 4
                int len = _buffer[2];
                int total = len + 4;
                if (total < 5 || total > MaxFrameBytes) {
                    // 非法长度：丢掉当前 0x55 重新找帧头
                    _buffer.RemoveAt(0);
                    continue;
                }
                if (_buffer.Count < total) {
                    break; // 半包，等下一次 Feed
                }

                // 3. 帧尾校验 0D 0A
                if (_buffer[total - 2] != 0x0D || _buffer[total - 1] != 0x0A) {
                    _buffer.RemoveAt(0);
                    continue;
                }

                frames.Add(_buffer.GetRange(0, total).ToArray());
                _buffer.RemoveRange(0, total);
            }

            if (_buffer.Count > MaxBufferBytes) {
                DroppedBytes += _buffer.Count;
                _buffer.Clear();
            }
            return frames;
        }

        public void Reset() => _buffer.Clear();
    }
}
