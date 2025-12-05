using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Abstracts;

namespace OperationGuidance_new.Tasks.DeviceTypes {
    public class IoBoxTypeArranger: AIoBoxDevice<IoBoxArranger> {
        private IoBoxTask _task;
        public Action<int?[]>? ActionAfterIoSignalReceived { get; set; } = null;

        public IoBoxTypeArranger(IoBoxTask task, IoBoxArranger deviceType, int deviceId) : base(deviceType, deviceId) => _task = task;

        public string WritePosition(int?[] position) => _task.SendCommand(DeviceType.GetWriteCommand(position).GetMessage());
        public void Reset() => _task.SendCommand(DeviceType.GetResetCommand().GetMessage());

        public async Task<string> SendPulseAsync(int?[] position, int pulseWidthMs = 200) {
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            if (pulseWidthMs < 0)
                throw new ArgumentOutOfRangeException(nameof(pulseWidthMs), "Pulse width cannot be negative.");

            try {
                // 1. 发送 Set 信号（在后台线程执行）
                string writeResult = await Task.Run(() =>
                    _task.SendCommand(DeviceType.GetWriteCommand(position).GetMessage()));

                // 2. 等待脉冲持续时间
                if (pulseWidthMs > 0) {
                    await Task.Delay(pulseWidthMs).ConfigureAwait(false);
                }

                // 3. 发送 Reset 信号（在后台线程执行）
                await Task.Run(() => _task.SendCommand(DeviceType.GetResetCommand().GetMessage()));

                return writeResult;

            } catch (Exception ex) {
                log.Error("Failed to send pulse command.", ex);
                throw;
            }
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
    }
}
