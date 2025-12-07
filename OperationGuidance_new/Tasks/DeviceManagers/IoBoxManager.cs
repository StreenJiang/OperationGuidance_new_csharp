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
        // 用于防止同一设备并发处理的锁字典（使用IP:Port作为键）
        private readonly ConcurrentDictionary<string, object> _deviceLocks = new();

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
                MainUtils.Info(_logger, "正在同步IoBox和Arm设备...", false);

                // 1. 移除已删除的设备
                RemoveDeletedDevices(ioBoxDtos, armDtos);

                // 2. 创建/更新活跃设备
                var tasks = new List<Task>();

                // 处理IoBox设备
                tasks.Add(Task.Run(async () => {
                    try {
                        foreach (var dto in ioBoxDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
                            int? workstationId = ioMaps.TryGetValue(dto.id, out var wsId) ? wsId : null;
                            CreateOrUpdateIoBoxDevice(dto, workstationId);
                        }
                    } catch (Exception ex) {
                        MainUtils.Error(_logger, $"处理IoBox设备时出错: {ex.Message}");
                    }
                }));

                // 处理Arm设备
                tasks.Add(Task.Run(async () => {
                    try {
                        foreach (var dto in armDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
                            int? workstationId = armMaps.TryGetValue(dto.id, out var wsId) ? wsId : null;
                            CreateOrUpdateArmDevice(dto, workstationId);
                        }
                    } catch (Exception ex) {
                        MainUtils.Error(_logger, $"处理Arm设备时出错: {ex.Message}");
                    }
                }));

                // 等待所有任务完成，设置30秒超时避免无限等待
                bool allCompleted = await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(TimeSpan.FromSeconds(30))) == tasks[0];
                if (!allCompleted) {
                    MainUtils.Warn(_logger, "IoBox和Arm设备同步超时（30秒）");
                }

                MainUtils.Info(_logger, "IoBox和Arm设备同步完成", false);
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
        public IoBoxTask? CreateOrUpdateIoBoxDevice(DeviceIoDTO dto, int? workstationId = null) {
            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
            // 获取或创建设备特定的锁，确保同一设备不会被并发处理
            var deviceLock = _deviceLocks.GetOrAdd(key, _ => new object());

            lock (deviceLock) {
                try {
                    IoBoxTask? task = GetExistingTask(key);

                if (task == null) {
                    // 获取设备显示名称
                    string deviceDisplayName = !string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "";

                    MainUtils.Info(_logger, $"正在创建IoBox设备: {dto.ip}:{dto.port} {deviceDisplayName}...", false);
                    task = MainUtils.NewIoBoxTask(dto.ip, dto.port);

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

                                if (task != null && task.Connected) {
                                    MainUtils.Info(_logger, $"✓ 成功连接到IoBox[{dto.ip}:{dto.port}] {deviceDisplayName}");
                                } else {
                                    MainUtils.Warn(_logger, $"✗ 连接到IoBox[{dto.ip}:{dto.port}] {deviceDisplayName} 失败");
                                }
                            } catch (Exception ex) {
                                MainUtils.Warn(_logger, $"检查IoBox[{dto.ip}:{dto.port}] {deviceDisplayName} 连接状态时出错: {ex.Message}");
                            }
                        });
                    } else {
                        MainUtils.Warn(_logger, $"创建IoBox设备 {dto.ip}:{dto.port} {deviceDisplayName} 失败");
                    }

                    return task;
                }

                // 更新现有设备
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
                        var newTask = MainUtils.NewIoBoxTask(dto.ip, dto.port);
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
                } catch (Exception ex) {
                    MainUtils.Error(_logger, $"创建/更新IoBox设备 {dto.ip}:{dto.port} 时出错: {ex.Message}");
                    return null;
                } finally {
                    // 处理完成后不清理锁字典，让GC负责清理
                }
            }
        }

        /// <summary>
        /// 创建或更新Arm设备
        /// </summary>
        public IoBoxTask? CreateOrUpdateArmDevice(DeviceArmDTO dto, int? workstationId = null) {
            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);
            // 获取或创建设备特定的锁，确保同一设备不会被并发处理
            var deviceLock = _deviceLocks.GetOrAdd(key, _ => new object());

            lock (deviceLock) {
                try {
                    IoBoxTask? task = GetExistingTask(key);

                if (task == null) {
                    MainUtils.Info(_logger, $"正在创建Arm设备: {dto.ip}:{dto.port}...", false);
                    task = MainUtils.NewIoBoxTask(dto.ip, dto.port);

                    if (task != null) {
                        AddTaskToCache(key, task);
                        MainUtils.Info(_logger, $"成功创建Arm设备 {dto.ip}:{dto.port}");
                        if (workstationId.HasValue) {
                            task.WorkstationId = workstationId.Value;
                        }
                    } else {
                        MainUtils.Warn(_logger, $"创建Arm设备 {dto.ip}:{dto.port} 失败");
                    }

                    return task;
                }

                // 更新现有设备
                if (workstationId.HasValue) {
                    task.WorkstationId = workstationId.Value;
                }

                // Arm设备目前不需要检查配置变更（简化处理）
                // 如果需要，可以在此添加 NeedsReconnection 逻辑

                // 检查是否需要重连
                if (!task.Connected && task.Status != ATaskBase.CONNECTING) {
                    MainUtils.Info(_logger, $"正在重连Arm {dto.ip}:{dto.port}", false);
                    _ = Task.Run(async () => await ReconnectAsync(task, $"Arm[{dto.ip}:{dto.port}]"));
                }

                return task;
                } catch (Exception ex) {
                    MainUtils.Error(_logger, $"创建/更新Arm设备 {dto.ip}:{dto.port} 时出错: {ex.Message}");
                    return null;
                } finally {
                    // 处理完成后不清理锁字典，让GC负责清理
                }
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
            // IoBox设备的type范围：1-4 (SetterSelector_4, SetterSelector_8, Arranger, SetterSelector_4_plus)
            // Arm设备的type范围：应该是不同的ID（需要根据DeviceType_Arm确定）
            // 由于IoBoxTask可以同时拥有多种类型，我们需要检查DTO.type是否与当前任务的任何类型匹配

            int dtoType = dto.type;
            bool typeMatches = false;

            // 检查DTO.type是否与当前任务的类型匹配
            // 如果DTO.type在1-4范围内，它应该匹配SetterSelectorType或ArrangerType
            if (dtoType >= 1 && dtoType <= 4) {
                // 检查Arranger类型 (ID=3)
                if (dtoType == 3 && task.ArrangerType?.DeviceType.Id == dtoType) {
                    typeMatches = true;
                }
                // 检查SetterSelector类型 (ID=1, 2, 4)
                else if (dtoType != 3 && task.SetterSelectorType?.DeviceType.Id == dtoType) {
                    typeMatches = true;
                }
            }
            // 如果DTO.type不在1-4范围内，可能是Arm类型或其他类型
            // 对于Arm类型，我们需要检查task.ArmType
            else if (task.ArmType?.DeviceType.Id == dtoType) {
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
