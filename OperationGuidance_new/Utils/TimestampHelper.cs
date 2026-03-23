using System.Buffers.Binary;

namespace OperationGuidance_new.Utils {
    public static class TimestampHelper {
        /// <summary>
        /// DateTime 转 4字节时间戳（小端）
        /// </summary>
        public static byte[] ToBytes(DateTime dt) {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, (int) new DateTimeOffset(dt).ToUnixTimeSeconds());
            return bytes;
        }

        /// <summary>
        /// 4字节时间戳（小端）转 DateTime
        /// </summary>
        public static DateTime ToDateTime(byte[] bytes) {
            return DateTimeOffset.FromUnixTimeSeconds(BinaryPrimitives.ReadInt32LittleEndian(bytes)).LocalDateTime;
        }
    }
}
