using log4net;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks.Interfaces;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Tasks.Abstracts {
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
        /// 获取设备名称（由子类实现）
        /// </summary>
        /// <param name="dto">设备DTO</param>
        /// <returns>设备名称</returns>
        protected abstract string? GetDeviceName(TDto dto);

        /// <summary>
        /// 获取设备信息（由子类实现）
        /// </summary>
        /// <param name="task">设备任务实例</param>
        /// <returns>设备信息描述</returns>
        protected abstract string GetDeviceInfoCore(TTask task);

        /// <summary>
        /// 获取设备显示名称（格式化：如果是空则返回空字符串，否则返回【名称】）
        /// </summary>
        /// <param name="dto">设备DTO</param>
        /// <returns>格式化的设备显示名称</returns>
        private string GetDeviceDisplayName(TDto dto) {
            string? deviceName = GetDeviceName(dto);
            return string.IsNullOrEmpty(deviceName) ? "" : $"【{deviceName}】";
        }

        /// <summary>
        /// 获取设备显示名称（格式化：如果是空则返回空字符串，否则返回【名称】）
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <returns>格式化的设备显示名称</returns>
        private string GetDeviceDisplayName(string? deviceName) {
            return string.IsNullOrEmpty(deviceName) ? "" : $"【{deviceName}】";
        }

        /// <summary>
        /// 关闭并清理设备任务
        /// </summary>
        /// <param name="task">设备任务实例</param>
        protected virtual void CleanupTask(TTask task) {
            try {
                RemoveTaskFromCache(task.DeviceId);
                task.CloseConnection();
                MainUtils.Info(Logger, $"任务 {GetDeviceInfoCore(task)} 已关闭");
            } catch (Exception ex) {
                MainUtils.Warn(Logger, $"关闭任务 {GetDeviceInfoCore(task)} 时出错: {ex.Message}");
            }
        }

        public virtual TTask? CreateOrUpdateDevice(TDto dto, int? workstationId = null) {
            try {
                // 获取或创建设备任务
                TTask? task = GetExistingTask(dto.id);

                if (task == null) {
                    // 获取设备显示名称
                    string deviceDisplayName = GetDeviceDisplayName(dto);

                    // 创建新设备
                    MainUtils.Info(Logger, $"正在创建新设备: {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName}...");
                    task = CreateTaskInstance(dto);

                    if (task != null) {
                        // Add to cache immediately after creation
                        AddTaskToCache(dto.id, task);
                        MainUtils.Info(Logger, $"成功创建 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} - {GetDeviceInfoCore(task)}");
                        if (workstationId.HasValue) {
                            task.WorkstationId = workstationId.Value;
                        }

                        // 显示连接状态日志给UI
                        _ = Task.Run(async () => {
                            try {
                                // 等待连接完成或超时
                                await Task.Delay(3000); // 最多等待3秒显示状态

                                if (task.Connected) {
                                    MainUtils.Info(Logger, $"✓ 成功连接到 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName}");
                                } else {
                                    MainUtils.Warn(Logger, $"✗ 连接到 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} 失败");
                                }
                            } catch (Exception ex) {
                                MainUtils.Warn(Logger, $"检查 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} 连接状态时出错: {ex.Message}");
                            }
                        });

                    } else {
                        MainUtils.Warn(Logger, $"创建 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} 失败");
                    }

                    return task;
                }

                // 更新现有设备
                if (workstationId.HasValue) {
                    task.WorkstationId = workstationId.Value;
                }

                // 检查是否需要重新创建设备
                if (NeedsReconnectionCore(task, dto)) {
                    // 获取设备显示名称
                    string deviceDisplayName = GetDeviceDisplayName(dto);

                    MainUtils.Info(Logger, $"{GetDeviceTypeName()} 配置已更改，正在重新创建 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName}...");
                    CleanupTask(task);

                    // 等待一段时间再重新创建（使用Thread.Sleep避免死锁风险）
                    System.Threading.Thread.Sleep(task.AutoReconnectingTrialDelay);

                    task = CreateTaskInstance(dto);
                    if (task != null) {
                        // Add to cache after recreation
                        AddTaskToCache(dto.id, task);
                        MainUtils.Info(Logger, $"成功重新创建 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} - {GetDeviceInfoCore(task)}");
                        if (workstationId.HasValue) {
                            task.WorkstationId = workstationId.Value;
                        }

                        // 显示连接状态日志给UI
                        _ = Task.Run(async () => {
                            try {
                                // 等待连接完成或超时
                                await Task.Delay(3000); // 最多等待3秒显示状态

                                if (task.Connected) {
                                    MainUtils.Info(Logger, $"✓ 成功连接到 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName}");
                                } else {
                                    MainUtils.Warn(Logger, $"✗ 连接到 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} 失败");
                                }
                            } catch (Exception ex) {
                                MainUtils.Warn(Logger, $"检查 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} 连接状态时出错: {ex.Message}");
                            }
                        });

                    } else {
                        MainUtils.Warn(Logger, $"重新创建 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} 失败");
                    }

                    return task;
                }

                // 设备配置未变，检查是否需要重连
                if (!task.Connected && task.Status != ATaskBase.CONNECTING) {
                    string deviceInfo = GetDeviceInfoCore(task);
                    // 获取设备显示名称
                    string deviceDisplayName = GetDeviceDisplayName(dto);
                    MainUtils.Info(Logger, $"正在重连 {GetDeviceTypeName()}[{dto.id}] {deviceDisplayName} - {deviceInfo}");
                    _ = Task.Run(async () => {
                        await ReconnectAsync(task, deviceInfo);
                    });
                }

                return task;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"创建/更新 {GetDeviceTypeName()}[{dto.id}] 时出错: {ex.Message}");
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
                        string deviceInfo = GetDeviceInfoCore(task);
                        MainUtils.Info(Logger, $"{GetDeviceTypeName()}[{deviceInfo}] 已被删除，正在移除...");
                        CleanupTask(task);
                        removedCount++;
                    }
                }
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"移除已删除的 {GetDeviceTypeName()} 设备时出错: {ex.Message}");
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
                        MainUtils.Info(Logger, $"{deviceInfo} - 正在重连... 第 {attempt}/{maxAttempts} 次尝试");
                    },
                    onAttemptFailed: () => {
                        MainUtils.Warn(Logger, $"{deviceInfo} - 重连尝试失败");
                    },
                    onAttemptSuccess: (result) => {
                        if (result) {
                            MainUtils.Info(Logger, $"{deviceInfo} - 重连成功");
                        }
                    }
                );

                return success;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"[{deviceInfo}] 重连时出错: {ex.Message}");
                return false;
            }
        }

        public virtual string GetDeviceInfo(TTask task) {
            return GetDeviceInfoCore(task);
        }

        public virtual async Task<int> SynchronizeDevicesAsync(IEnumerable<TDto> dtos, Dictionary<int, int> workstationMap) {
            try {
                // Synchronizing logs only go to backend, not UI
                MainUtils.Info(Logger, $"正在同步 {GetDeviceTypeName()} 设备...", false);

                // 1. 移除已删除的设备
                int removedCount = RemoveDeletedDevices(dtos);
                if (removedCount > 0) {
                    MainUtils.Info(Logger, $"已移除 {removedCount} 个已删除的 {GetDeviceTypeName()} 设备");
                }

                // 2. 创建/更新活跃设备
                var tasks = dtos.Select(async dto => {
                    int? workstationId = workstationMap.TryGetValue(dto.id, out var wsId) ? wsId : null;
                    return CreateOrUpdateDevice(dto, workstationId);
                });

                var results = await Task.WhenAll(tasks);
                int processedCount = results.Count(r => r != null);

                MainUtils.Info(Logger, $"成功同步 {processedCount} 个 {GetDeviceTypeName()} 设备", false);

                return processedCount;
            } catch (Exception ex) {
                MainUtils.Error(Logger, $"同步 {GetDeviceTypeName()} 设备时出错: {ex.Message}");
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

        /// <summary>
        /// 添加任务到缓存（由子类实现）
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="task">设备任务实例</param>
        protected abstract void AddTaskToCache(int deviceId, TTask task);

        #endregion
    }
}
