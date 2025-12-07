namespace OperationGuidance_new.Tasks.Abstracts {
    public abstract class ATaskBase {
        #region Readonly fields
        private int _deviceId;
        private int? _workstationId;
        protected string? _device_name = "";
        protected readonly int LoopingInterval = 100;
        public readonly int AutoReconnectingTrialDelay = 500; // 断线重连尝试间隔
        public static readonly int DISCONNECTED = 0;
        public static readonly int CONNECTING = 1;
        public static readonly int CONNECTED = 2;
        #endregion

        #region Properties
        public int DeviceId => _deviceId;
        public int? WorkstationId { get => _workstationId; set => _workstationId = value; }
        public string Name => _device_name ?? "";
        public abstract bool Connected { get; }
        public int Status { get; set; }
        public bool CloseConnectionManually { get; set; } = false;
        #endregion

        public ATaskBase(int deviceId, int? workstationId = null) {
            _deviceId = deviceId;
            _workstationId = workstationId;
        }

        #region Main methods (Async - Primary)
        /// <summary>
        /// 执行主要任务循环 - 异步版本
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        protected abstract Task RunTaskAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步连接到设备
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接结果</returns>
        public abstract Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步关闭连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        public abstract Task CloseConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查工作场所连接状态
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接状态</returns>
        public abstract Task<bool> WorkplaceCheckConnectionAsync(CancellationToken cancellationToken = default);
        #endregion

        #region Legacy methods (Synchronous - Deprecated)
        /// <summary>
        /// 执行主要任务循环 - 同步版本（已弃用，请使用 RunTaskAsync）
        /// </summary>
        [Obsolete("Use RunTaskAsync with CancellationToken instead")]
        protected virtual void RunTask() {
            try {
                RunTaskAsync().GetAwaiter().GetResult();
            }
            catch (AggregateException ex) {
                throw ex.InnerException ?? ex;
            }
        }

        /// <summary>
        /// 连接到设备 - 同步版本（已弃用，请使用 ConnectAsync）
        /// </summary>
        [Obsolete("Use ConnectAsync with CancellationToken instead")]
        public virtual void Connect() {
            try {
                ConnectAsync().GetAwaiter().GetResult();
            }
            catch (AggregateException ex) {
                throw ex.InnerException ?? ex;
            }
        }

        /// <summary>
        /// 关闭连接 - 同步版本（已弃用，请使用 CloseConnectionAsync）
        /// </summary>
        [Obsolete("Use CloseConnectionAsync with CancellationToken instead")]
        public virtual void CloseConnection() {
            try {
                CloseConnectionAsync().GetAwaiter().GetResult();
            }
            catch (AggregateException ex) {
                throw ex.InnerException ?? ex;
            }
        }

        /// <summary>
        /// 检查工作场所连接状态 - 同步版本（已弃用，请使用 WorkplaceCheckConnectionAsync）
        /// </summary>
        [Obsolete("Use WorkplaceCheckConnectionAsync with CancellationToken instead")]
        public virtual bool WorkplaceCheckConnection() {
            try {
                return WorkplaceCheckConnectionAsync().GetAwaiter().GetResult();
            }
            catch (AggregateException ex) {
                throw ex.InnerException ?? ex;
            }
        }
        #endregion

        #region Helper methods for subclasses
        /// <summary>
        /// 标准连接重试循环
        /// </summary>
        /// <param name="connectFunc">连接函数</param>
        /// <param name="onConnected">连接成功回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接结果</returns>
        protected async Task<bool> ConnectWithRetryAsync(
            Func<CancellationToken, Task<bool>> connectFunc,
            Action? onConnected = null,
            CancellationToken cancellationToken = default) {
            try {
                while (!cancellationToken.IsCancellationRequested && !Connected) {
                    Status = CONNECTING;

                    if (await connectFunc(cancellationToken)) {
                        Status = CONNECTED;
                        onConnected?.Invoke();
                        return true;
                    }

                    await Task.Delay(AutoReconnectingTrialDelay, cancellationToken);
                }
                return false;
            }
            catch (OperationCanceledException) {
                logger?.Info($"连接已取消 - 设备 {Name}");
                return false;
            }
            catch (Exception ex) {
                logger?.Error($"连接过程中发生错误 - 设备 {Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// 标准任务循环
        /// </summary>
        /// <param name="loopFunc">循环函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        protected async Task RunTaskLoopAsync(
            Func<CancellationToken, Task> loopFunc,
            CancellationToken cancellationToken = default) {
            try {
                while (!cancellationToken.IsCancellationRequested && Connected) {
                    await loopFunc(cancellationToken);
                    await Task.Delay(LoopingInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException) {
                logger?.Info($"任务循环已取消 - 设备 {Name}");
            }
            catch (Exception ex) {
                logger?.Error($"任务循环发生错误 - 设备 {Name}", ex);
            }
            finally {
                logger?.Info($"任务循环结束 - 设备 {Name}");
            }
        }
        #endregion

        #region Logging support
        /// <summary>
        /// 日志记录器（子类可重写）
        /// </summary>
        protected virtual log4net.ILog? logger => null;
        #endregion
    }
}
