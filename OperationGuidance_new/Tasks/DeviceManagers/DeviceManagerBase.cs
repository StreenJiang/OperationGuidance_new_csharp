using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.AbstractClasses;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Tasks.DeviceManagers {
    /// <summary>
    /// 设备管理器基类
    /// 提供通用的设备同步逻辑，子类只需实现具体的创建逻辑
    /// </summary>
    /// <typeparam name="TDto">设备DTO类型</typeparam>
    /// <typeparam name="TTask">设备任务类型</typeparam>
    public abstract class DeviceManagerBase<TDto, TTask> : IDeviceManager<TDto, TTask>
        where TDto : ADTOBase
        where TTask : ATaskBase {

        protected readonly ILog Logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        protected DeviceManagerBase(ILog logger) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建设备任务实例（由子类实现）
        /// </summary>
        /// <param name="dto">设备DTO</param>
        /// <returns>设备任务实例，如果无法创建则返回null</returns>
        protected abstract TTask? CreateTaskInstance(TDto dto);

        /// <summary>
        /// 获取设备类型名称（用于日志）
        /// </summary>
        /// <returns>设备类型名称</returns>
        protected abstract string GetDeviceTypeName();

        /// <summary>
        /// 检查设备是否需要重新连接（由子类实现）
        /// 子类可以自定义重连条件
        /// </summary>
        /// <param name="task">设备任务实例</param>
        /// <param name="dto">对应的DTO</param>
        /// <returns>是否需要重连</returns>
        protected abstract bool NeedsReconnectionCore(TTask task, TDto dto);

        /// <summary>
        /// 获取设备信息（由子类实现）
        /// </summary>
        /// <param name="task">设备任务实例</param>
        /// <returns>设备信息描述</returns>
        protected abstract string GetDeviceInfoCore(TTask task);

        /// <summary>
        /// 关闭并清理设备任务
        /// </summary>
        /// <param name="task">设备任务实例</param>
        protected virtual void CleanupTask(TTask task) {
            try {
                task.CloseConnection();
                MainUtils.Info(Logger, $"Task for {GetDeviceInfoCore(task)} has been closed.");
            } catch (Exception ex) {
                MainUtils.Warn(Logger, $"Error closing task for {GetDeviceInfoCore(task)}: {ex.Message}");
            }
        }

        public virtual TTask? CreateOrUpdateDevice(TDto dto, int? workstationId = null) {
            try {
                // 获取或创建设备任务
                TTask? task = GetExistingTask(dto.id);

                if (task == null) {
                    // 创建新设备
                    MainUtils.Info(Logger, $"Creating new device: {GetDeviceTypeName()}[{dto.id}]...");
                    task = CreateTaskInstance(dto);

                    if (task != null) {
                        MainUtils.Info(Logger, $"Successfully created {GetDeviceTypeName()}[{dto.id}]");
                        if (workstationId.HasValue) {
                            task.WorkstationId = workstationId.Value;
                        }
                    } else {
                        MainUtils.Warn(Logger, $"Failed to create {GetDeviceTypeName()}[{dto.id}]");
                    }

                    return task;
                }

                // 更新现有设备
                if (workstationId.HasValue) {
                    task.WorkstationId = workstationId.Value;
                }

                // 检查是否需要重新创建设备
                if (NeedsReconnectionCore(task, dto)) {
                    MainUtils.Info(Logger, $"{GetDeviceTypeName()} configuration changed, recreating device...");
                    CleanupTask(task);
                    RemoveTaskFromCache(task.DeviceId);

                    // 等待一段时间再重新创建（同步等待，避免async复杂性）
                    Task.Delay(task.AutoReconnectingTrialDelay).GetAwaiter().GetResult();

                    task = CreateTaskInstance(dto);
                    if (task != null && workstationId.HasValue) {
                        task.WorkstationId = workstationId.Value;
                    }

                    return task;
                }

                // 设备配置未变，检查是否需要重连
                if (!task.Connected && task.Status != ATaskBase.CONNECTING) {
                    string deviceInfo = GetDeviceInfoCore(task);
                    _ = Task.Run(async () => {
                        await ReconnectAsync(task, deviceInfo);
                    });
                }

                return task;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"Error creating/updating {GetDeviceTypeName()}[{dto.id}]: {ex.Message}");
                return null;
            }
        }

        public virtual int RemoveDeletedDevices(IEnumerable<TDto> activeDtos) {
            int removedCount = 0;
            var activeIds = new HashSet<int>(activeDtos.Select(dto => dto.id));

            try {
                // 获取所有缓存的任务
                var allTasks = GetAllCachedTasks();

                foreach (var task in allTasks) {
                    if (!activeIds.Contains(task.DeviceId)) {
                        MainUtils.Info(Logger, $"{GetDeviceTypeName()}[{GetDeviceInfoCore(task)}] is marked as deleted, removing...");
                        CleanupTask(task);
                        RemoveTaskFromCache(task.DeviceId);
                        removedCount++;
                    }
                }
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"Error removing deleted {GetDeviceTypeName()} devices: {ex.Message}");
            }

            return removedCount;
        }

        public virtual bool NeedsReconnection(TTask task, TDto dto) {
            return NeedsReconnectionCore(task, dto);
        }

        public virtual async Task<bool> ReconnectAsync(TTask task, string deviceInfo) {
            try {
                // 使用重试策略进行重连
                var retryStrategy = RetryStrategy.IncrementalDelay(
                    maxAttempts: 3,
                    baseDelayMs: 500,
                    maxDelayMs: 3000
                );

                bool success = await retryStrategy.ExecuteAsync(
                    operation: async () => {
                        if (!task.Connected && task.Status != ATaskBase.CONNECTING) {
                            task.Connect();
                            // 等待连接完成
                            await Task.Delay(500);
                            return task.Connected;
                        }
                        return task.Connected;
                    },
                    onAttemptProgress: (attempt, maxAttempts) => {
                        MainUtils.Info(Logger, $"[{deviceInfo}] Reconnecting... Attempt {attempt}/{maxAttempts}");
                    },
                    onAttemptFailed: () => {
                        MainUtils.Warn(Logger, $"[{deviceInfo}] Reconnection attempt failed");
                    },
                    onAttemptSuccess: (result) => {
                        if (result) {
                            MainUtils.Info(Logger, $"[{deviceInfo}] Reconnected successfully");
                        }
                    }
                );

                return success;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"[{deviceInfo}] Error during reconnection: {ex.Message}");
                return false;
            }
        }

        public virtual string GetDeviceInfo(TTask task) {
            return GetDeviceInfoCore(task);
        }

        public virtual async Task<int> SynchronizeDevicesAsync(IEnumerable<TDto> dtos, Dictionary<int, int> workstationMap) {
            try {
                MainUtils.Info(Logger, $"Synchronizing {GetDeviceTypeName()} devices...");

                // 1. 移除已删除的设备
                int removedCount = RemoveDeletedDevices(dtos);
                if (removedCount > 0) {
                    MainUtils.Info(Logger, $"Removed {removedCount} deleted {GetDeviceTypeName()} devices");
                }

                // 2. 创建/更新活跃设备
                var tasks = dtos.Select(async dto => {
                    int? workstationId = workstationMap.TryGetValue(dto.id, out var wsId) ? wsId : null;
                    return CreateOrUpdateDevice(dto, workstationId);
                });

                var results = await Task.WhenAll(tasks);
                int processedCount = results.Count(r => r != null);

                MainUtils.Info(Logger, $"Successfully synchronized {processedCount} {GetDeviceTypeName()} devices");

                return processedCount;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"Error synchronizing {GetDeviceTypeName()} devices: {ex.Message}");
                throw;
            }
        }

        #region Abstract Methods for Cache Management

        /// <summary>
        /// 获取现有任务（由子类实现）
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>设备任务实例，如果不存在则返回null</returns>
        protected abstract TTask? GetExistingTask(int deviceId);

        /// <summary>
        /// 获取所有缓存的任务（由子类实现）
        /// </summary>
        /// <returns>所有设备任务的集合</returns>
        protected abstract IEnumerable<TTask> GetAllCachedTasks();

        /// <summary>
        /// 从缓存中移除任务（由子类实现）
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        protected abstract void RemoveTaskFromCache(int deviceId);

        #endregion
    }
}
