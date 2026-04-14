using log4net;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using S7.Net;
using System.Text;

namespace OperationGuidance_new.Constants {
    public class PlcServer_GLB: PlcServerBase {
        private ILog log = LogManager.GetLogger(typeof(PlcConfig_GLB));
        public PlcConfig_GLB PlcConfig { get; set; }
        private const int MaxRetries = 3;          // 最大重试次数
        private const int DelayMs = 100;           // 每次重试间隔 100ms


        public PlcServer_GLB(CpuType cpuType,
                             string ip,
                             PlcConfig_GLB plcCnfig) : base(cpuType, ip) {
            PlcConfig = plcCnfig;
        }

        // 1. 先读条码
        public string? ReadBarCode() {
            var config = PlcConfig.BarCodeConfig();

            byte[] bytes = ReadBytes(config);
            return Encoding.ASCII.GetString(bytes);
        }

        // 2. 发送条码读取成功信号
        public void SendBarCodeReadDone() => WriteBool(PlcConfig.BarCodeDoneConfig(), true);
        // 2.1 重置条码读取成功信号
        public void ResetBarCodeReadDone() => WriteBool(PlcConfig.BarCodeDoneConfig(), false);

        // 3. 读取开始任务信号
        public bool ReadStartSignal() {
            var config = PlcConfig.StartSignalConfig();

            if (config.BitOffset < 0 || config.BitOffset > 7) {
                throw new ArgumentException("BitOffset must be between 0 and 7.");
            }

            byte[] bytes = ReadBytes(config);
            return (bytes[0] & (1 << config.BitOffset)) != 0;
        }

        // 4. 发送任务完成信号
        public void SendJobFinished(bool val) => WriteBool(PlcConfig.JobFinishedConfig(), val);

        private byte[] ReadBytes(PlcTagConfig_GLB config) {
            if (Plc == null || !Plc.IsConnected)
                throw new InvalidOperationException("PLC is not connected.");

            for (int attempt = 1; attempt <= MaxRetries; attempt++) {
                try {
                    byte[]? bytes = Plc.ReadBytes(config.DataType,
                                                  config.BlockNumber,
                                                  config.ByteOffset,
                                                  config.Length);

                    if (bytes?.Length == config.Length) {
                        string hex = BitConverter.ToString(bytes).Replace("-", " ");
                        string binary = MainUtils.ToBinaryString(bytes);
                        log.Info($"Read from PLC Target: {config.DataType} {config.BlockNumber}, " +
                                 $"ByteOffset={config.ByteOffset}, BitOffset={config.BitOffset}, " +
                                 $"Length={bytes.Length}, value = [hex='{hex}', binary='{binary}']");
                        return bytes;
                    }
                } catch (Exception) when (attempt < MaxRetries) {
                    Thread.Sleep(DelayMs);
                }
            }

            throw new InvalidOperationException(
                $"Failed to read data from PLC after {MaxRetries} retries. " +
                $"Target: {config.DataType} {config.BlockNumber}, Byte={config.ByteOffset}, Bit={config.BitOffset}");
        }

        private void WriteBool(PlcTagConfig_GLB config, bool val) {
            if (Plc == null)
                throw new InvalidOperationException("PLC is not connected.");

            for (int attempt = 1; attempt <= MaxRetries; attempt++) {
                try {
                    Plc.WriteBit(config.DataType,
                              config.BlockNumber,
                              config.ByteOffset,
                              config.BitOffset,
                              val);

                    return; // 成功，退出
                } catch (Exception ex) when (attempt < MaxRetries) {
                    log.Error($"Write failed (attempt {attempt}), retrying... {ex.Message}");
                    Thread.Sleep(DelayMs);
                }
            }

            throw new InvalidOperationException(
                $"Failed to write bool to PLC after {MaxRetries} retries. " +
                $"Target: {config.DataType} {config.BlockNumber}, Byte={config.ByteOffset}, Bit={config.BitOffset}");
        }
    }
}
