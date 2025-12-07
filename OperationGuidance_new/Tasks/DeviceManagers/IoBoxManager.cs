using log4net;
using OperationGuidance_new.Tasks.Abstracts;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using System.Collections.Concurrent;

namespace OperationGuidance_new.Tasks.DeviceManagers {
    /// <summary>
    /// IoBox和Arm设备管理器
    /// 负责IoBoxTask的创建、更新、删除和重连
    /// 支持两种设备类型：DeviceIoDTO (Arranger/SetterSelector) 和 DeviceArmDTO (Arm)
    /// 多个设备类型可以共享同一个IoBoxTask（基于IP:Port）
    /// </summary>
    public class IoBoxManager {
        private readonly ILog _logger;
        private readonly ConcurrentDictionary<string, IoBoxTask> _tasks = new();
        // 用于防止同一设备并发处理的信号量字典（使用IP:Port作为键）
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _deviceSemaphores = new();

        public IoBoxManager() {
            _logger = MainUtils.GetLogger(typeof(IoBoxManager));
        }

        /// <summary>
        /// 同步IoBox和Arm设备
        /// </summary>
        public async Task<int> SynchronizeDevicesAsync(
            IEnumerable<DeviceIoDTO> ioBoxDtos,
            IEnumerable<DeviceArmDTO> armDtos,
            Dictionary<int, int> ioMaps,
            Dictionary<int, int> armMaps) {
            try {
                var activeIoBoxCount = ioBoxDtos.Count(d => d.deleted == (int) YesOrNo.NO);
                var activeArmCount = armDtos.Count(d => d.deleted == (int) YesOrNo.NO);
                MainUtils.Info(_logger, $"正在同步IoBox和Arm设备... IoBox: {activeIoBoxCount}个, Arm: {activeArmCount}个", false);

                // 1. 移除已删除的设备
                RemoveDeletedDevices(ioBoxDtos, armDtos);

                // 2. 创建/更新活跃设备 - 使用真正的异步操作
                var ioBoxTask = Task.Run(async () => {
                    try {
                        MainUtils.Info(_logger, $"开始处理 {activeIoBoxCount} 个IoBox设备...", false);
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        foreach (var dto in ioBoxDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
                            int? workstationId = ioMaps.TryGetValue(dto.id, out var wsId) ? wsId : null;
                            await CreateOrUpdateIoBoxDevice(dto, workstationId);
                        }
                        stopwatch.Stop();
                        MainUtils.Info(_logger, $"完成处理 {activeIoBoxCount} 个IoBox设备，耗时: {stopwatch.ElapsedMilliseconds}ms", false);
                    } catch (Exception ex) {
                        MainUtils.Error(_logger, $"处理IoBox设备时出错: {ex.Message}");
                    }
                });

                var armTask = Task.Run(async () => {
                    try {
                        MainUtils.Info(_logger, $"开始处理 {activeArmCount} 个Arm设备...", false);
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        foreach (var dto in armDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
                            int? workstationId = armMaps.TryGetValue(dto.id, out var wsId) ? wsId : null;
                            await CreateOrUpdateArmDevice(dto, workstationId);
                        }
                        stopwatch.Stop();
                        MainUtils.Info(_logger, $"完成处理 {activeArmCount} 个Arm设备，耗时: {stopwatch.ElapsedMilliseconds}ms", false);
                    } catch (Exception ex) {
                        MainUtils.Error(_logger, $"处理Arm设备时出错: {ex.Message}");
                    }
                });

                // 等待所有任务完成，设置30秒超时避免无限等待
                var allTasks = Task.WhenAll(ioBoxTask, armTask);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var completedTask = await Task.WhenAny(allTasks, timeoutTask);
                bool allCompleted = completedTask == allTasks;

                if (!allCompleted) {
                    MainUtils.Warn(_logger, "IoBox和Arm设备同步超时（30秒）");
                } else {
                    MainUtils.Info(_logger, "IoBox和Arm设备同步在超时前完成", false);
                }
                return _tasks.Count;
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"同步IoBox和Arm设备时出错: {ex.Message}");
                // 返回当前任务数量，不抛出异常以避免阻塞主循环
                return _tasks.Count;
            }
        }

        /// <summary>
        /// 创建或更新IoBox设备（Arranger或SetterSelector）
        /// </summary>
        public async Task<IoBoxTask?> CreateOrUpdateIoBoxDevice(DeviceIoDTO dto, int? workstationId = null) {
            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
            // 快速路径：检查现有任务，避免不必要的锁
            var task = GetExistingTask(key);
            if (task != null) {
                if (workstationId.HasValue) {
                    task.WorkstationId = workstationId.Value;
                }

                // 检查是否需要重新连接（IP地址、端口或设备类型改变）
                if (NeedsReconnection(task, dto)) {
                    string deviceDisplayName = !string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "";
                    MainUtils.Info(_logger, $"IoBox配置已更改，正在重新创建 {dto.ip}:{dto.port} {deviceDisplayName}...");
                    CleanupTask(task);

                    // 移除阻塞性Thread.Sleep，直接重新创建设备
                    // 延迟重新创建逻辑移到后台异步执行
                    _ = Task.Run(async () => {
                        if (task.AutoReconnectingTrialDelay > 0) {
                            await Task.Delay(task.AutoReconnectingTrialDelay);
                        }
                        var newTask = await MainUtils.NewIoBoxTaskAsync(dto.ip, dto.port, dto.type, dto.id);
                        if (newTask != null) {
                            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
                            AddTaskToCache(key, newTask);
                            MainUtils.Info(_logger, $"成功重新创建IoBox设备 {dto.ip}:{dto.port} {deviceDisplayName}");
                            if (workstationId.HasValue) {
                                newTask.WorkstationId = workstationId.Value;
                            }
                        }
                    });

                    // 返回null表示设备将在后台重新创建
                    return null;
                }

                // 设备配置未变，检查是否需要重连
                if (!task.Connected && task.Status != ATaskBase.CONNECTING) {
                    string deviceDisplayName = !string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "";
                    MainUtils.Info(_logger, $"正在重连IoBox {dto.ip}:{dto.port} {deviceDisplayName}", false);
                    _ = Task.Run(async () => await ReconnectAsync(task, $"IoBox[{dto.ip}:{dto.port}] {deviceDisplayName}"));
                }

                return task;
            }

            // 获取或创建设备特定的信号量，确保同一设备不会被并发处理
            var deviceSemaphore = _deviceSemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await deviceSemaphore.WaitAsync();
            try {
                // 双重检查：信号量内再次检查
                task = GetExistingTask(key);
                if (task != null) {
                    if (workstationId.HasValue) {
                        task.WorkstationId = workstationId.Value;
                    }
                    return task;
                }

                // 获取设备显示名称
                string deviceDisplayName = !string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "";

                MainUtils.Info(_logger, $"正在创建IoBox设备: {dto.ip}:{dto.port} {deviceDisplayName}...", false);
                // 使用异步方法创建并连接任务
                task = await MainUtils.NewIoBoxTaskAsync(dto.ip, dto.port, dto.type, dto.id);

                if (task != null) {
                    AddTaskToCache(key, task);
                    MainUtils.Info(_logger, $"成功创建IoBox设备 {dto.ip}:{dto.port} {deviceDisplayName}");
                    if (workstationId.HasValue) {
                        task.WorkstationId = workstationId.Value;
                    }

                    // 显示连接状态日志给UI（后台执行）
                    _ = Task.Run(async () => {
                        try {
                            await Task.Delay(3000); // 最多等待3秒显示状态

                            // 使用try-catch确保后台任务不会崩溃
                            try {
                                if (task != null && task.Connected) {
                                    MainUtils.Info(_logger, $"✓ 成功连接到IoBox[{dto.ip}:{dto.port}] {(!string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "")}");
                                } else {
                                    MainUtils.Warn(_logger, $"✗ 连接到IoBox[{dto.ip}:{dto.port}] {(!string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "")} 失败");
                                }
                            } catch (Exception innerEx) {
                                // 只记录异常，不抛出，避免影响后台任务
                                MainUtils.Warn(_logger, $"检查IoBox[{dto.ip}:{dto.port}] {(!string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "")} 连接状态时出错: {innerEx.Message}");
                            }
                        } catch {
                            // 忽略所有后台任务异常
                        }
                    });
                } else {
                    MainUtils.Warn(_logger, $"创建IoBox设备 {dto.ip}:{dto.port} {deviceDisplayName} 失败");
                }

                return task;
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"创建/更新IoBox设备 {dto.ip}:{dto.port} 时出错: {ex.Message}");
                return null;
            } finally {
                // 释放信号量
                deviceSemaphore.Release();
            }
        }

        /// <summary>
        /// 创建或更新Arm设备
        /// </summary>
        public async Task<IoBoxTask?> CreateOrUpdateArmDevice(DeviceArmDTO dto, int? workstationId = null) {
            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
            // 快速路径：检查现有任务，避免不必要的锁
            var task = GetExistingTask(key);
            if (task != null) {
                if (workstationId.HasValue) {
                    task.WorkstationId = workstationId.Value;
                }

                // 检查是否需要重连
                if (!task.Connected && task.Status != ATaskBase.CONNECTING) {
                    MainUtils.Info(_logger, $"正在重连Arm {dto.ip}:{dto.port}", false);
                    _ = Task.Run(async () => await ReconnectAsync(task, $"Arm[{dto.ip}:{dto.port}]"));
                }

                return task;
            }

            // 获取或创建设备特定的信号量，确保同一设备不会被并发处理
            var deviceSemaphore = _deviceSemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await deviceSemaphore.WaitAsync();
            try {
                // 双重检查：信号量内再次检查
                task = GetExistingTask(key);
                if (task != null) {
                    if (workstationId.HasValue) {
                        task.WorkstationId = workstationId.Value;
                    }
                    return task;
                }

                MainUtils.Info(_logger, $"正在创建Arm设备: {dto.ip}:{dto.port}...", false);
                // 使用异步方法创建并连接任务
                task = await MainUtils.NewIoBoxTaskAsync(dto.ip, dto.port, dto.type, dto.id);

                if (task != null) {
                    AddTaskToCache(key, task);
                    MainUtils.Info(_logger, $"成功创建Arm设备 {dto.ip}:{dto.port}");
                    if (workstationId.HasValue) {
                        task.WorkstationId = workstationId.Value;
                    }

                    // 显示连接状态日志给UI（后台执行）
                    _ = Task.Run(async () => {
                        try {
                            await Task.Delay(3000); // 最多等待3秒显示状态

                            // 使用try-catch确保后台任务不会崩溃
                            try {
                                if (task != null && task.Connected) {
                                    MainUtils.Info(_logger, $"✓ 成功连接到Arm[{dto.ip}:{dto.port}] {(!string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "")}");
                                } else {
                                    MainUtils.Warn(_logger, $"✗ 连接到Arm[{dto.ip}:{dto.port}] {(!string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "")} 失败");
                                }
                            } catch (Exception innerEx) {
                                // 只记录异常，不抛出，避免影响后台任务
                                MainUtils.Warn(_logger, $"检查Arm[{dto.ip}:{dto.port}] {(!string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "")} 连接状态时出错: {innerEx.Message}");
                            }
                        } catch {
                            // 忽略所有后台任务异常
                        }
                    });
                } else {
                    MainUtils.Warn(_logger, $"创建Arm设备 {dto.ip}:{dto.port} 失败");
                    return null;
                }

                return task;
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"创建/更新Arm设备 {dto.ip}:{dto.port} 时出错: {ex.Message}");
                return null;
            } finally {
                // 释放信号量
                deviceSemaphore.Release();
            }
        }

        /// <summary>
        /// 移除已删除的设备
        /// </summary>
        public void RemoveDeletedDevices(
            IEnumerable<DeviceIoDTO> activeIoBoxDtos,
            IEnumerable<DeviceArmDTO> activeArmDtos) {
            try {
                var activeKeys = new HashSet<string>();

                // 构建活跃设备键集合
                foreach (var dto in activeIoBoxDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
                    activeKeys.Add(MainUtils.GetTCPClientKey(dto.ip, dto.port));
                }

                foreach (var dto in activeArmDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
                    activeKeys.Add(MainUtils.GetTCPClientKey(dto.ip, dto.port));
                }

                // 移除不再活跃的设备
                var keysToRemove = _tasks.Keys.Where(key => !activeKeys.Contains(key)).ToList();
                foreach (var key in keysToRemove) {
                    if (_tasks.TryRemove(key, out var task)) {
                        CleanupTask(task);
                    }
                }

                if (keysToRemove.Count > 0) {
                    MainUtils.Info(_logger, $"已移除 {keysToRemove.Count} 个已删除的IoBox/Arm设备");
                }
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"移除已删除的IoBox/Arm设备时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查设备是否需要重新连接
        /// </summary>
        private bool NeedsReconnection(IoBoxTask task, DeviceIoDTO dto) {
            // 检查IP地址、端口是否改变
            bool needsReconnect = task.Ip != dto.ip || task.Port != dto.port;

            // 检查设备类型是否改变
            int dtoType = dto.type;
            bool typeMatches = false;

            // 直接比较DTO.type与任务中设备类型的ID
            // IoBox设备的type范围：1-4 (SetterSelector_4, SetterSelector_8, Arranger, SetterSelector_4_plus)
            // Arm设备的type范围：1-4 (CF01, CF02, CF03, CF04)
            if (task.ArmType?.DeviceType.Id == dtoType) {
                typeMatches = true;
            } else if (task.ArrangerType?.DeviceType.Id == dtoType) {
                typeMatches = true;
            } else if (task.SetterSelectorType?.DeviceType.Id == dtoType) {
                typeMatches = true;
            }

            // 如果类型不匹配，也需要重连
            if (!typeMatches) {
                needsReconnect = true;
                // 记录类型不匹配的详细信息
                string currentTypeInfo = "无";
                if (task.ArrangerType != null) {
                    currentTypeInfo = $"Arranger(ID={task.ArrangerType.DeviceType.Id})";
                } else if (task.SetterSelectorType != null) {
                    currentTypeInfo = $"SetterSelector(ID={task.SetterSelectorType.DeviceType.Id})";
                } else if (task.ArmType != null) {
                    currentTypeInfo = $"Arm(ID={task.ArmType.DeviceType.Id})";
                }

                MainUtils.Info(_logger, $"IoBox[{dto.ip}:{dto.port}] 需要重连 - " +
                    $"IP变化: {task.Ip} -> {dto.ip}, " +
                    $"Port变化: {task.Port} -> {dto.port}, " +
                    $"Type变化: {currentTypeInfo} -> ID={dtoType}", false);
            }

            return needsReconnect;
        }

        /// <summary>
        /// 重新连接设备
        /// </summary>
        private async Task ReconnectAsync(IoBoxTask task, string deviceInfo) {
            try {
                await task.ConnectAsync();
                MainUtils.Info(_logger, $"已重新连接到 {deviceInfo}", false);
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"重新连接 {deviceInfo} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭并清理设备任务
        /// </summary>
        private void CleanupTask(IoBoxTask task) {
            try {
                task.CloseConnection();
                MainUtils.Info(_logger, $"任务 {GetDeviceInfo(task)} 已关闭");
            } catch (Exception ex) {
                MainUtils.Warn(_logger, $"关闭任务 {GetDeviceInfo(task)} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取设备信息
        /// </summary>
        private string GetDeviceInfo(IoBoxTask task) {
            return $"{task.Ip}:{task.Port}";
        }

        /// <summary>
        /// 获取现有任务
        /// </summary>
        public IoBoxTask? GetExistingTask(string key) {
            return MainUtils.TryGetIoBoxTask(key);
        }

        /// <summary>
        /// 获取所有缓存的任务
        /// </summary>
        public IEnumerable<IoBoxTask> GetAllCachedTasks() {
            return _tasks.Values.ToList();
        }

        /// <summary>
        /// 从缓存中移除任务
        /// </summary>
        public void RemoveTaskFromCache(string key) {
            _tasks.TryRemove(key, out _);
        }

        /// <summary>
        /// 添加任务到缓存
        /// </summary>
        public void AddTaskToCache(string key, IoBoxTask task) {
            _tasks[key] = task;
        }
    }
}
