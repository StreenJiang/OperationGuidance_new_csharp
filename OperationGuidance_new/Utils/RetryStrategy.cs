using System;
using System.Threading;
using System.Threading.Tasks;

namespace OperationGuidance_new.Utils {
    /// <summary>
    /// 通用的异步重试策略类
    /// 支持多种重试场景：固定延迟、指数退避、递增延迟等
    /// 支持回调通知：重试进度、失败通知、成功通知
    /// </summary>
    public class RetryStrategy {
        private readonly int _maxAttempts;
        private readonly TimeSpan _baseDelay;
        private readonly RetryDelayStrategy _delayStrategy;
        private readonly bool _failFast;
        private readonly TimeSpan _maxDelay;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxAttempts">最大重试次数</param>
        /// <param name="baseDelayMs">基础延迟时间（毫秒）</param>
        /// <param name="delayStrategy">延迟策略</param>
        /// <param name="failFast">是否快速失败（遇到第一次失败就返回）</param>
        /// <param name="maxDelayMs">最大延迟时间（毫秒），防止延迟过长</param>
        public RetryStrategy(
            int maxAttempts = 5,
            int baseDelayMs = 1000,
            RetryDelayStrategy delayStrategy = RetryDelayStrategy.Incremental,
            bool failFast = false,
            int maxDelayMs = 30000) {
            _maxAttempts = maxAttempts;
            _baseDelay = TimeSpan.FromMilliseconds(baseDelayMs);
            _delayStrategy = delayStrategy;
            _failFast = failFast;
            _maxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
        }

        /// <summary>
        /// 执行重试操作
        /// </summary>
        /// <param name="operation">要执行的操作</param>
        /// <param name="onAttemptProgress">进度回调 (currentAttempt, maxAttempts)</param>
        /// <param name="onAttemptFailed">失败回调</param>
        /// <param name="onAttemptSuccess">成功回调</param>
        /// <param name="token">取消令牌</param>
        /// <returns>操作是否成功</returns>
        public async Task<bool> ExecuteAsync(
            Func<Task<bool>> operation,
            Action<int, int>? onAttemptProgress = null,
            Action? onAttemptSuccess = null,
            Action? onAttemptFailed = null,
            CancellationToken token = default) {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt < _maxAttempts && !token.IsCancellationRequested) {
                try {
                    attempt++;

                    // 在每次尝试开始时报告进度，避免重复调用
                    onAttemptProgress?.Invoke(attempt, _maxAttempts);

                    // 执行操作
                    bool result = await operation();
                    if (result) {
                        // 成功回调
                        onAttemptSuccess?.Invoke();
                        return true;
                    }

                    // 操作失败，如果是最后一次尝试则返回
                    onAttemptFailed?.Invoke();

                    if (attempt >= _maxAttempts) {
                        return false;
                    }

                    // 计算延迟时间
                    TimeSpan delay = CalculateDelay(attempt);

                    await Task.Delay(delay, token);
                } catch (OperationCanceledException) {
                    return false;
                } catch (Exception ex) {
                    lastException = ex;
                    onAttemptFailed?.Invoke();

                    if (attempt >= _maxAttempts) {
                        break;
                    }

                    TimeSpan delay = CalculateDelay(attempt);

                    try {
                        await Task.Delay(delay, token);
                    } catch (OperationCanceledException) {
                        return false;
                    }
                }
            }

            throw new RetryException($"操作在 {_maxAttempts} 次尝试后失败", lastException);
        }

        /// <summary>
        /// 计算延迟时间
        /// </summary>
        /// <param name="attempt">当前尝试次数</param>
        /// <returns>延迟时间</returns>
        private TimeSpan CalculateDelay(int attempt) {
            TimeSpan delay = _delayStrategy switch {
                RetryDelayStrategy.Fixed => _baseDelay,
                RetryDelayStrategy.Incremental => TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * attempt),
                RetryDelayStrategy.Exponential => TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)),
                RetryDelayStrategy.Linear => TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds + (attempt - 1) * 100),
                _ => _baseDelay
            };

            // 限制最大延迟时间，防止过长延迟
            return TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, _maxDelay.TotalMilliseconds));
        }

        /// <summary>
        /// 创建一个固定延迟的重试策略
        /// </summary>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <param name="delayMs">延迟时间（毫秒）</param>
        /// <returns>重试策略实例</returns>
        public static RetryStrategy FixedDelay(int maxAttempts, int delayMs) {
            return new RetryStrategy(maxAttempts, delayMs, RetryDelayStrategy.Fixed);
        }

        /// <summary>
        /// 创建一个递增延迟的重试策略
        /// </summary>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <param name="baseDelayMs">基础延迟时间（毫秒）</param>
        /// <param name="maxDelayMs">最大延迟时间（毫秒）</param>
        /// <returns>重试策略实例</returns>
        public static RetryStrategy IncrementalDelay(int maxAttempts, int baseDelayMs = 1000, int maxDelayMs = 10000) {
            return new RetryStrategy(maxAttempts, baseDelayMs, RetryDelayStrategy.Incremental, false, maxDelayMs);
        }

        /// <summary>
        /// 创建一个指数退避的重试策略
        /// </summary>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <param name="baseDelayMs">基础延迟时间（毫秒）</param>
        /// <param name="maxDelayMs">最大延迟时间（毫秒）</param>
        /// <returns>重试策略实例</returns>
        public static RetryStrategy ExponentialBackoff(int maxAttempts, int baseDelayMs = 1000, int maxDelayMs = 15000) {
            return new RetryStrategy(maxAttempts, baseDelayMs, RetryDelayStrategy.Exponential, false, maxDelayMs);
        }

        /// <summary>
        /// 创建一个快速失败的重试策略
        /// </summary>
        /// <param name="maxAttempts">最大尝试次数</param>
        /// <param name="baseDelayMs">基础延迟时间（毫秒）</param>
        /// <returns>重试策略实例</returns>
        public static RetryStrategy FailFast(int maxAttempts = 1, int baseDelayMs = 0) {
            return new RetryStrategy(maxAttempts, baseDelayMs, RetryDelayStrategy.Fixed, true);
        }
    }

    /// <summary>
    /// 重试延迟策略枚举
    /// </summary>
    public enum RetryDelayStrategy {
        /// <summary>
        /// 固定延迟：每次重试间隔相同
        /// </summary>
        Fixed,

        /// <summary>
        /// 递增延迟：每次重试延迟时间递增 (delay * attempt)
        /// </summary>
        Incremental,

        /// <summary>
        /// 指数退避：每次重试延迟时间指数增长 (delay * 2^(attempt-1))
        /// </summary>
        Exponential,

        /// <summary>
        /// 线性递增：每次重试延迟时间线性增长
        /// </summary>
        Linear
    }

    /// <summary>
    /// 重试异常类
    /// </summary>
    public class RetryException: Exception {
        public RetryException(string message) : base(message) { }

        public RetryException(string message, Exception? innerException)
            : base(message, innerException) { }
    }
}
