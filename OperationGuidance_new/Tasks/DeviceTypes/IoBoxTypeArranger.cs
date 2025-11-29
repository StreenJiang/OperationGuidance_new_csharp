using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AsbtractClasses;

namespace OperationGuidance_new.Tasks.DeviceTypes {
    public class IoBoxTypeArranger: AIoBoxDevice<IoBoxArranger> {
        private IoBoxTask _task;
        public Action<int?[]>? ActionAfterIoSignalReceived { get; set; } = null;

        public IoBoxTypeArranger(IoBoxTask task, IoBoxArranger deviceType, int deviceId) : base(deviceType, deviceId) => _task = task;

        public string WritePosition(int?[] position) => _task.SendCommand(DeviceType.GetWriteCommand(position).GetMessage());
        public void Reset() => _task.SendCommand(DeviceType.GetResetCommand().GetMessage());

        public async Task<string> SendPulseAsync(int?[] position, int pulseWidthMs = 200) {
            // 参数校验
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            if (pulseWidthMs < 0)
                throw new ArgumentOutOfRangeException(nameof(pulseWidthMs), "Pulse width cannot be negative.");

            // 1. 发送 Set 信号（例如：置位 IO 或写入位置）
            string writeResult = _task.SendCommand(DeviceType.GetWriteCommand(position).GetMessage());

            // 2. 等待脉冲持续时间
            if (pulseWidthMs > 0) {
                await Task.Delay(pulseWidthMs);
            }

            // 3. 发送 Reset 信号（清零，形成下降沿）
            try {
                _task.SendCommand(DeviceType.GetResetCommand().GetMessage());
            } catch (Exception ex) {
                // 记录 Reset 失败，但不抛出（脉冲已有效发出）
                log.Error("Failed to send reset command after pulse.", ex);
            }

            return writeResult;
        }

        public string SendPulse(int?[] position, int pulseWidthMs = 200) {
            return SendPulseAsync(position, pulseWidthMs).GetAwaiter().GetResult();
        }

        public Tuple<int?[], int?[]> ReadCurrent() {
            // 发送‘读取’命令
            string readResult = _task.SendCommand(DeviceType.COMMAND_READ.GetMessage());
            // 分析结果
            DeviceType.AnalyzeReadResultData(readResult);
            // 返回当前读取到的结果
            return DeviceType.GetCurrent();
        }

        public void OpenDoor(int?[] position) => WritePosition(position);
    }
}
