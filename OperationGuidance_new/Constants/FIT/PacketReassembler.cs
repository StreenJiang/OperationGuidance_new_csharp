using log4net;
using System.Buffers.Binary;

namespace OperationGuidance_new.Constants.FIT {
    public class PacketReassembler {
        private ILog log = LogManager.GetLogger(typeof(PacketReassembler));
        private readonly Dictionary<int, CompleteTighteningCurve> _activeCurves = new();
        private readonly Dictionary<int, CurvePoint[][]> _packetBuffers = new();
        private readonly object _lock = new object();

        /// <summary>
        /// 处理接收到的数据包
        /// </summary>
        public CompleteTighteningCurve? ProcessReceivedData(byte[] data) {
            lock (_lock) {
                // 1. 解析数据包
                var packet = ParsePacket(data);
                if (packet == null)
                    return null;

                // 2. 初始化或获取重组缓冲区
                if (!_packetBuffers.ContainsKey(packet.TighteningId)) {
                    // 新的拧紧任务
                    _packetBuffers[packet.TighteningId] = new CurvePoint[packet.TotalPackets][];
                    _activeCurves[packet.TighteningId] = new CompleteTighteningCurve {
                        TighteningId = packet.TighteningId,
                        ReceiveStartTime = DateTime.Now
                    };
                }

                // 3. 存储当前包的数据
                _packetBuffers[packet.TighteningId][packet.CurrentPacket - 1] = packet.Points.ToArray();

                // 4. 更新最后接收时间
                _activeCurves[packet.TighteningId].LastUpdateTime = DateTime.Now;

                // 5. 检查是否接收完成
                if (IsReceiveComplete(packet.TighteningId, packet.TotalPackets)) {
                    var completeCurve = AssembleCompleteCurve(packet.TighteningId);
                    Cleanup(packet.TighteningId);
                    return completeCurve;
                }
                return null; // 还未接收完成
            }
        }

        /// <summary>
        /// 解析单个数据包
        /// </summary>
        private TighteningCurvePacket? ParsePacket(byte[] data) {
            Span<byte> span = data.AsSpan();
            int offset = 3; // 帧头(2) + 命令字(1)

            var packet = new TighteningCurvePacket();
            try {
                ushort dataLen = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
                log.Debug($"dataLen={dataLen}");
                offset += 2;

                // 拧紧ID (4字节，小端)
                packet.TighteningId = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
                log.Debug($"TighteningId={packet.TighteningId}");
                offset += 4;

                // 总包数 (2字节)
                packet.TotalPackets = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
                log.Debug($"TotalPackets={packet.TotalPackets}");
                offset += 2;

                // 当前包号 (2字节)
                packet.CurrentPacket = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
                log.Debug($"CurrentPacket={packet.CurrentPacket}");
                offset += 2;

                // 解析曲线点（每包最多20个点）
                int pointsCount = (data.Length - offset) / 12; // 每个点12字节 (4+4+4)

                for (int i = 0; i < pointsCount; i++) {
                    var point = new CurvePoint {
                        Time = BitConverter.ToUInt32(data, offset),
                        Torque = BitConverter.ToSingle(data, offset + 4),
                        Angle = BitConverter.ToSingle(data, offset + 8)
                    };
                    packet.Points.Add(point);
                    offset += 12;
                }

                return packet;
            } catch (Exception ex) {
                log.Debug($"解析数据包失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查是否接收完成
        /// </summary>
        private bool IsReceiveComplete(int tighteningId, ushort totalPackets) {
            var buffers = _packetBuffers[tighteningId];

            // 检查所有包是否都已收到
            for (int i = 0; i < totalPackets; i++) {
                if (buffers[i] == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 组装完整曲线
        /// </summary>
        private CompleteTighteningCurve AssembleCompleteCurve(int tighteningId) {
            var curve = _activeCurves[tighteningId];
            var buffers = _packetBuffers[tighteningId];

            foreach (var packetPoints in buffers) {
                if (packetPoints != null)
                    curve.AllPoints.AddRange(packetPoints);
            }

            return curve;
        }

        /// <summary>
        /// 清理缓冲区
        /// </summary>
        private void Cleanup(int tighteningId) {
            _packetBuffers.Remove(tighteningId);
            _activeCurves.Remove(tighteningId);
        }

        /// <summary>
        /// 清理超时未完成的重组（建议定时调用）
        /// </summary>
        public void CleanupTimeouts(TimeSpan timeout) {
            lock (_lock) {
                var now = DateTime.Now;
                var toRemove = _activeCurves
                    .Where(kvp => now - kvp.Value.LastUpdateTime > timeout)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var id in toRemove) {
                    _packetBuffers.Remove(id);
                    _activeCurves.Remove(id);
                    log.Debug($"清理超时的拧紧ID: {id}");
                }
            }
        }
    }
}
