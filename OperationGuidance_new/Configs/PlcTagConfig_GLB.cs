using log4net;
using S7.Net;

namespace OperationGuidance_new.Configs {
    /// <summary>
    /// PLC 标签配置（纯配置，不含转换逻辑）
    /// </summary>
    public class PlcTagConfig_GLB {
        private static ILog log = LogManager.GetLogger(typeof(PlcTagConfig_GLB));

        // ====== 通信地址 ======
        public DataType DataType { get; set; }          // DB / M / I / Q
        public int BlockNumber { get; set; }            // DB 块号
        public int BitOffset { get; set; }              // 位偏移
        public int Length { get; set; }                 // 长度（字节）
        public int ByteOffset { get; set; }             // 如 DBB606

        /// <summary>
        /// 从设备地址字符串解析（4或5字段）
        /// </summary>
        public static PlcTagConfig_GLB FromDeviceAddress(string deviceAddress) {
            if (string.IsNullOrWhiteSpace(deviceAddress))
                throw new ArgumentException("设备地址不能为空");

            var parts = deviceAddress.Split(',');
            if (parts.Length < 5)
                throw new ArgumentException("设备地址格式错误，应为：[存储区],[DB号],[偏移量],[长度],[符号名]");

            PlcTagConfig_GLB config = new PlcTagConfig_GLB {
                DataType = ParseS7DataType(parts[0]),
                BlockNumber = int.Parse(parts[1]),
                BitOffset = int.Parse(parts[2]),
                Length = int.Parse(parts[3]),
                ByteOffset = int.Parse(parts[4].Substring(3))
            };
            log.Info($"Config [{parts}] loaded...");
            return config;
        }

        private static DataType ParseS7DataType(string typeStr) {
            return typeStr.ToUpperInvariant() switch {
                "DB" => DataType.DataBlock,
                "M" => DataType.Memory,
                "I" => DataType.Input,
                "Q" => DataType.Output,
                _ => throw new ArgumentException($"不支持的存储区类型: {typeStr}")
            };
        }
    }
}
