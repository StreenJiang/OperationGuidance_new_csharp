using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;

namespace OperationGuidance_new.Tasks.DeviceTypes {
    public class IoBoxTypeSetterSelector: AIoBoxDevice<IoBoxSetterSelector> {
        protected IoBoxTask _task;
        public Action<int>? ActionAfterIoSignalReceived { get; set; } = null;

        private const int MaxRetryAttempts = 10;
        private const int RetryDelayMs = 100;

        public IoBoxTypeSetterSelector(IoBoxTask task, IoBoxSetterSelector deviceType, int deviceId) : base(deviceType, deviceId) => _task = task;

        public virtual string WritePosition(int position) => _task.SendCommand(DeviceType.GetWriteCommand(position).GetMessage());

        /// <summary>
        /// 重置设备状态（异步版本，推荐使用）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的Task</returns>
        public virtual async Task ResetAsync(CancellationToken cancellationToken = default) {
            if (_task == null) {
                throw new InvalidOperationException("Task is not initialized");
            }

            string resetCommand = DeviceType.GetResetCommand().GetMessage();
            string result = _task.SendCommand(resetCommand);

            bool success = await WaitForResetCompleteAsync(result, cancellationToken);

            if (!success) {
                throw new TimeoutException($"Reset operation failed after {MaxRetryAttempts} attempts");
            }
        }

        /// <summary>
        /// 重置设备状态（同步版本，为向后兼容保留）
        /// </summary>
        /// <remarks>
        /// 此方法为向后兼容而保留。建议使用 ResetAsync() 获得更好的异步支持。
        /// 使用 ConfigureAwait(false) 避免潜在的死锁风险。
        /// </remarks>
        public virtual void Reset() {
            try {
                // 使用 ConfigureAwait(false) 避免死锁，特别是UI线程中的调用
                ResetAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            } catch (AggregateException ex) {
                // 展开AggregateException获取实际异常
                throw ex.InnerException ?? ex;
            } catch (Exception) {
                // 直接重新抛出其他异常（包括OperationCanceledException、TaskCanceledException等）
                throw;
            }
        }

        /// <summary>
        /// 等待重置完成
        /// </summary>
        /// <param name="initialResult">初始重置命令的结果</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重置是否成功</returns>
        private async Task<bool> WaitForResetCompleteAsync(string initialResult, CancellationToken cancellationToken) {
            int attempt = 0;

            // 修复：条件错误 - 应该是 !ok（当写入未成功时继续重试）
            while (attempt < MaxRetryAttempts && !cancellationToken.IsCancellationRequested) {
                bool writeOk = DeviceType.WriteOk(initialResult);
                int currentStatus = DeviceType.CurrentStatus;

                // 检查重置是否完成（写入成功且状态为0）
                if (writeOk && currentStatus == 0) {
                    return true;
                }

                attempt++;

                if (attempt < MaxRetryAttempts) {
                    await Task.Delay(RetryDelayMs, cancellationToken);
                }
            }

            return false;
        }
    }
}
