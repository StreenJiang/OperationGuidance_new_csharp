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

                // 步骤1：创建/更新活跃设备 - 先处理IoBox和Arm设备
                // 这样可以避免配置变更的设备被误删
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

                // 步骤3：移除已删除的设备（基于设备ID匹配）
                // 在处理完所有活跃设备后执行，避免误删配置变更的设备
                MainUtils.Info(_logger, "正在清理已删除的设备...", false);
                RemoveDeletedDevices(ioBoxDtos, armDtos);

                // 步骤4：清理orphaned tasks（不再被任何活跃设备使用的IP:Port任务）
                MainUtils.Info(_logger, "正在清理orphaned tasks...", false);
                CleanupOrphanedTasks(ioBoxDtos, armDtos);

                return MainUtils.IoBoxTasks.Count;
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"同步IoBox和Arm设备时出错: {ex.Message}");
                // 返回当前任务数量，不抛出异常以避免阻塞主循环
                return MainUtils.IoBoxTasks.Count;
            }
        }

        /// <summary>
        /// 创建或更新IoBox设备（Arranger或SetterSelector）
        /// </summary>
        public async Task<IoBoxTask?> CreateOrUpdateIoBoxDevice(DeviceIoDTO dto, int? workstationId = null) {
            string key = MainUtils.GetTCPClientKey(dto.ip, dto.port);

            // 获取设备显示名称
            string deviceDisplayName = !string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "";

            // 先使用 ID 查找对应的 task，如果找到了再检查 key 是否匹配，可以避免配置修改后找到别的 task 从而忘记从原本的 task 移除 device id
            var task = FindTaskByDeviceId(dto.id);
            if (task != null) {
                string oldKey = MainUtils.GetTCPClientKey(task.Ip, task.Port);
                // Check if IP:Port actually changed
                if (oldKey != key) {
                    MainUtils.Info(_logger, $"Found task by DeviceId={dto.id}, {deviceDisplayName} at {oldKey}, config has changed", false);
                    MainUtils.Info(_logger, $"IoBox设备 {deviceDisplayName} 配置更改：{oldKey} -> {key}");
                    // Config changed - handle migration
                    HandleConfigChange(task, oldKey, key, dto.id);

                    // Get task from new key
                    task = GetExistingTask(key);
                } else {
                    // Same keys, so no need to fetch again
                    MainUtils.Info(_logger, $"通过设备ID找到IoBox任务: {task.Ip}:{task.Port} {deviceDisplayName} (DeviceId={dto.id})", false);
                }
            }

            if (task != null) {
                if (workstationId.HasValue) {
                    task.WorkstationId = workstationId.Value;
                }
                task.AddDeviceId(dto.id);

                // 检查是否需要重新连接（IP地址、端口或设备类型改变）
                if (NeedsReconnection(task, dto)) {
                    string oldKey = MainUtils.GetTCPClientKey(task.Ip, task.Port);
                    string newKey = MainUtils.GetTCPClientKey(dto.ip, dto.port);

                    MainUtils.Info(_logger, $"IoBox配置已更改，正在重新创建: {oldKey} -> {newKey} {deviceDisplayName}...");

                    // 清理旧任务（先移除缓存，再关闭连接）
                    RemoveTaskFromCacheWithMapping(oldKey, task);
                    CleanupTask(task);

                    // 移除阻塞性Thread.Sleep，直接重新创建设备
                    // 延迟重新创建逻辑移到后台异步执行
                    _ = Task.Run(async () => {
                        if (task.AutoReconnectingTrialDelay > 0) {
                            await Task.Delay(task.AutoReconnectingTrialDelay);
                        }
                        var newTask = await MainUtils.NewIoBoxTaskAsync(dto.ip, dto.port, dto.type, false, dto.id);
                        if (newTask != null) {
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
                    task.AddDeviceId(dto.id);
                    return task;
                }

                MainUtils.Info(_logger, $"正在创建IoBox设备: {dto.ip}:{dto.port} {deviceDisplayName}...", false);
                // 使用异步方法创建并连接任务
                task = await MainUtils.NewIoBoxTaskAsync(dto.ip, dto.port, dto.type, false, dto.id);

                if (task != null) {
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
                                    MainUtils.Info(_logger, $"✓ 成功连接到IoBox[{dto.ip}:{dto.port}] {deviceDisplayName}");
                                } else {
                                    MainUtils.Warn(_logger, $"✗ 连接到IoBox[{dto.ip}:{dto.port}] {deviceDisplayName} 失败");
                                }
                            } catch (Exception innerEx) {
                                // 只记录异常，不抛出，避免影响后台任务
                                MainUtils.Warn(_logger, $"检查IoBox[{dto.ip}:{dto.port}] {deviceDisplayName} 连接状态时出错: {innerEx.Message}");
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
                MainUtils.Error(_logger, $"创建/更新IoBox设备 {dto.ip}:{dto.port} {deviceDisplayName} 时出错: {ex.Message}");
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

            // 获取设备显示名称
            string deviceDisplayName = !string.IsNullOrEmpty(dto.name) ? $"【{dto.name}】" : "";

            // 先使用 ID 查找对应的 task，如果找到了再检查 key 是否匹配，可以避免配置修改后找到别的 task 从而忘记从原本的 task 移除 device id
            var task = FindTaskByDeviceId(dto.id);
            if (task != null) {
                string oldKey = MainUtils.GetTCPClientKey(task.Ip, task.Port);
                // Check if IP:Port actually changed
                if (oldKey != key) {
                    MainUtils.Info(_logger, $"Found task by DeviceId={dto.id}, {deviceDisplayName} at {oldKey}, config has changed", false);
                    MainUtils.Info(_logger, $"Arm设备 {deviceDisplayName} 配置更改：{oldKey} -> {key}");
                    // Config changed - handle migration
                    HandleConfigChange(task, oldKey, key, dto.id);

                    // Get task from new key
                    task = GetExistingTask(key);
                } else {
                    // Same keys, so no need to fetch again
                    MainUtils.Info(_logger, $"通过设备ID找到Arm任务: {task.Ip}:{task.Port} {deviceDisplayName} (DeviceId={dto.id})", false);
                }
            }

            if (task != null) {
                if (workstationId.HasValue) {
                    task.WorkstationId = workstationId.Value;
                }
                task.AddDeviceId(dto.id);

                // 检查是否需要重新连接（IP地址、端口或设备类型改变）
                if (NeedsReconnection(task, dto)) {
                    string oldKey = MainUtils.GetTCPClientKey(task.Ip, task.Port);
                    string newKey = MainUtils.GetTCPClientKey(dto.ip, dto.port);

                    MainUtils.Info(_logger, $"Arm配置已更改，正在重新创建: {oldKey} -> {newKey} {deviceDisplayName}...");

                    // 清理旧任务（先移除缓存，再关闭连接）
                    RemoveTaskFromCacheWithMapping(oldKey, task);
                    CleanupTask(task);

                    // 移除阻塞性Thread.Sleep，直接重新创建设备
                    // 延迟重新创建逻辑移到后台异步执行
                    _ = Task.Run(async () => {
                        if (task.AutoReconnectingTrialDelay > 0) {
                            await Task.Delay(task.AutoReconnectingTrialDelay);
                        }
                        var newTask = await MainUtils.NewIoBoxTaskAsync(dto.ip, dto.port, dto.type, true, dto.id);
                        if (newTask != null) {
                            MainUtils.Info(_logger, $"成功重新创建Arm设备 {dto.ip}:{dto.port} {deviceDisplayName}");
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
                    MainUtils.Info(_logger, $"正在重连Arm {dto.ip}:{dto.port} {deviceDisplayName}", false);
                    _ = Task.Run(async () => await ReconnectAsync(task, $"Arm[{dto.ip}:{dto.port}] {deviceDisplayName}"));
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
                    task.AddDeviceId(dto.id);
                    return task;
                }

                MainUtils.Info(_logger, $"正在创建Arm设备: {dto.ip}:{dto.port} {deviceDisplayName}...", false);
                // 使用异步方法创建并连接任务
                task = await MainUtils.NewIoBoxTaskAsync(dto.ip, dto.port, dto.type, true, dto.id);

                if (task != null) {
                    MainUtils.Info(_logger, $"成功创建Arm设备 {dto.ip}:{dto.port} {deviceDisplayName}");
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
                                    MainUtils.Info(_logger, $"✓ 成功连接到Arm[{dto.ip}:{dto.port}] {deviceDisplayName}");
                                } else {
                                    MainUtils.Warn(_logger, $"✗ 连接到Arm[{dto.ip}:{dto.port}] {deviceDisplayName} 失败");
                                }
                            } catch (Exception innerEx) {
                                // 只记录异常，不抛出，避免影响后台任务
                                MainUtils.Warn(_logger, $"检查Arm[{dto.ip}:{dto.port}] {deviceDisplayName} 连接状态时出错: {innerEx.Message}");
                            }
                        } catch {
                            // 忽略所有后台任务异常
                        }
                    });
                } else {
                    MainUtils.Warn(_logger, $"创建Arm设备 {dto.ip}:{dto.port} {deviceDisplayName} 失败");
                    return null;
                }

                return task;
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"创建/更新Arm设备 {dto.ip}:{dto.port} {deviceDisplayName} 时出错: {ex.Message}");
                return null;
            } finally {
                // 释放信号量
                deviceSemaphore.Release();
            }
        }

        /// <summary>
        /// 移除已删除的设备（基于IP:Port键判断，支持IoBox和Arm共享IoBoxTask）
        /// </summary>
        public void RemoveDeletedDevices(
            IEnumerable<DeviceIoDTO> activeIoBoxDtos,
            IEnumerable<DeviceArmDTO> activeArmDtos) {
            try {
                // 构建活跃的设备ID集合（区分IoBox和Arm设备ID空间）
                var activeIoBoxIds = activeIoBoxDtos
                    .Where(d => d.deleted == (int) YesOrNo.NO)
                    .Select(d => d.id)
                    .ToHashSet();

                var activeArmIds = activeArmDtos
                    .Where(d => d.deleted == (int) YesOrNo.NO)
                    .Select(d => d.id)
                    .ToHashSet();

                // 使用锁保护整个移除操作，避免竞态条件
                lock (MainUtils.IoBoxTasks) {
                    var tasksToRemove = new List<KeyValuePair<string, IoBoxTask>>();

                    foreach (var kvp in MainUtils.IoBoxTasks) {
                        var task = kvp.Value;
                        var key = kvp.Key;

                        // 检查主设备ID是否活跃
                        bool primaryIsActive = activeIoBoxIds.Contains(task.DeviceId) ||
                                               activeArmIds.Contains(task.DeviceId);

                        // 移除不活跃的设备ID
                        var deviceIdsToRemove = task.DeviceIds
                            .Where(id => !activeIoBoxIds.Contains(id) && !activeArmIds.Contains(id))
                            .ToList();

                        foreach (var deviceId in deviceIdsToRemove) {
                            // Lock on the task instance to ensure thread-safe device ID removal
                            lock (task) {
                                task.RemoveDeviceId(deviceId);
                            }
                            MainUtils.Info(_logger, $"Removed deleted device {deviceId} from task at {key}", false);
                        }

                        // 检查任务是否应该被移除
                        bool hasActiveDevices = primaryIsActive || task.DeviceIds.Any();

                        if (!hasActiveDevices) {
                            tasksToRemove.Add(kvp);
                        } else {
                            MainUtils.Info(_logger, $"Task at {key} kept alive with {task.DeviceIds.Count} device(s)", false);
                        }
                    }

                    // 执行移除
                    foreach (var kvp in tasksToRemove) {
                        if (MainUtils.IoBoxTasks.TryRemove(kvp.Key, out var task)) {
                            string deviceInfo = $"{task.Ip}:{task.Port}";
                            string deviceType = task.ArmType != null ? "Arm" : "IoBox";
                            // 清理信号量
                            CleanupSemaphoreForDevice(kvp.Key);
                            CleanupTask(task);
                            MainUtils.Info(_logger, $"Task removed: {deviceInfo} ({deviceType}, DeviceId={task.DeviceId})", false);
                        }
                    }

                    if (tasksToRemove.Count > 0) {
                        MainUtils.Info(_logger, $"已移除 {tasksToRemove.Count} 个无设备的IoBox/Arm任务");
                    }
                }
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"移除已删除的IoBox/Arm设备时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理orphaned tasks（不再被任何活跃设备使用的IP:Port任务）
        /// 当设备修改IP/Port后，旧IP/Port对应的任务如果没有其他设备使用，就会成为orphaned task
        /// 修复：移除DeviceId <= 0的检查（DeviceId是只读属性，永远不会<=0）
        /// </summary>
        private void CleanupOrphanedTasks(
            IEnumerable<DeviceIoDTO> activeIoBoxDtos,
            IEnumerable<DeviceArmDTO> activeArmDtos) {
            try {
                // 构建活跃键集合
                var activeKeys = new HashSet<string>();
                foreach (var dto in activeIoBoxDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
                    activeKeys.Add(MainUtils.GetTCPClientKey(dto.ip, dto.port));
                }
                foreach (var dto in activeArmDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
                    activeKeys.Add(MainUtils.GetTCPClientKey(dto.ip, dto.port));
                }

                // 构建当前所有任务的键集合
                var taskKeys = new HashSet<string>();
                lock (MainUtils.IoBoxTasks) {
                    foreach (var kvp in MainUtils.IoBoxTasks) {
                        taskKeys.Add(kvp.Key);
                    }
                }

                // 找出真正orphaned的键（存在于任务中但不存在于活跃设备中）
                var orphanedKeys = taskKeys.Except(activeKeys).ToList();

                // 执行清理（扩展锁范围避免竞态条件）
                foreach (var key in orphanedKeys) {
                    // 使用嵌套锁确保TryRemove和task检查的原子性
                    lock (MainUtils.IoBoxTasks) {
                        if (MainUtils.IoBoxTasks.TryRemove(key, out var task)) {
                            lock (task) {
                                // 验证任务确实无人使用（只检查DeviceIds集合）
                                // 移除task.DeviceId <= 0的检查，因为DeviceId是只读属性永远不会<=0
                                if (!task.DeviceIds.Any()) {
                                    CleanupTask(task);
                                    MainUtils.Info(_logger, $"Cleaned up orphaned task: {key}", false);
                                } else {
                                    // 有附加设备在使用，重新添加回去
                                    MainUtils.IoBoxTasks[key] = task;
                                }
                            }
                        }
                    }
                }

                if (orphanedKeys.Count > 0) {
                    MainUtils.Info(_logger, $"已清理 {orphanedKeys.Count} 个orphaned IoBox/Arm任务");
                }
            } catch (Exception ex) {
                MainUtils.Error(_logger, $"清理orphaned tasks时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理设备的信号量
        /// </summary>
        private void CleanupSemaphoreForDevice(string key) {
            _deviceSemaphores.TryRemove(key, out _);
        }

        /// <summary>
        /// 清理所有未使用的信号量
        /// </summary>
        public void CleanupUnusedSemaphores() {
            try {
                var activeKeys = MainUtils.IoBoxTasks.Keys.ToHashSet();
                var keysToRemove = _deviceSemaphores.Keys.Where(k => !activeKeys.Contains(k)).ToList();

                foreach (var key in keysToRemove) {
                    _deviceSemaphores.TryRemove(key, out _);
                }

                if (keysToRemove.Count > 0) {
                    MainUtils.Info(_logger, $"已清理 {keysToRemove.Count} 个未使用的信号量", false);
                }
            } catch (Exception ex) {
                MainUtils.Warn(_logger, $"清理信号量时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查IoBox设备是否需要重新连接
        /// </summary>
        private bool NeedsReconnection(IoBoxTask task, DeviceIoDTO dto) {
            // 检查IP地址、端口是否改变
            bool needsReconnect = task.Ip != dto.ip || task.Port != dto.port;

            // 检测是否为共享任务（任务中存在Arm设备类型）
            bool isSharedTask = task.ArmType != null;

            // 检查设备类型是否改变
            int dtoType = dto.type;
            bool typeMatches = false;

            // 获取当前设备类型信息（用于日志）
            string currentTypeInfo = "无";
            if (task.ArrangerType != null) {
                currentTypeInfo = $"Arranger(ID={task.ArrangerType.DeviceType.Id})";
            } else if (task.SetterSelectorType != null) {
                currentTypeInfo = $"SetterSelector(ID={task.SetterSelectorType.DeviceType.Id})";
            }

            // 直接比较DTO.type与任务中设备类型的ID
            // IoBox设备的type范围：1-4 (SetterSelector_4, SetterSelector_8, Arranger, SetterSelector_4_plus)
            if (task.ArrangerType?.DeviceType.Id == dtoType) {
                typeMatches = true;
            } else if (task.SetterSelectorType?.DeviceType.Id == dtoType) {
                typeMatches = true;
            }

            // 共享任务场景：如果IP/Port未变，即使类型缺失也不重连
            // 这是因为共享任务中设备类型会在使用时自动初始化
            if (isSharedTask && !needsReconnect) {
                // 共享任务且IP/Port未变，不需要重连
                return false;
            }

            // 独立任务场景：如果类型不匹配，需要重连
            if (!typeMatches) {
                needsReconnect = true;
            }

            if (needsReconnect) {
                MainUtils.Info(_logger, $"IoBox[{dto.ip}:{dto.port}] 需要重连 - " +
                    $"IP变化: {task.Ip} -> {dto.ip}, " +
                    $"Port变化: {task.Port} -> {dto.port}, " +
                    $"Type变化: {currentTypeInfo} -> ID={dtoType}" +
                    (isSharedTask ? " [共享任务]" : " [独立任务]"));
            }
            return needsReconnect;
        }

        /// <summary>
        /// 检查Arm设备是否需要重新连接
        /// </summary>
        private bool NeedsReconnection(IoBoxTask task, DeviceArmDTO dto) {
            // 检查IP地址、端口是否改变
            bool needsReconnect = task.Ip != dto.ip || task.Port != dto.port;

            // 检测是否为共享任务（任务中存在IoBox设备类型）
            bool isSharedTask = task.ArrangerType != null || task.SetterSelectorType != null;

            // 检查设备类型是否改变
            int dtoType = dto.type;
            bool typeMatches = false;

            // 获取当前设备类型信息（用于日志）
            string currentTypeInfo = "无";
            if (task.ArmType != null) {
                currentTypeInfo = $"Arm(ID={task.ArmType.DeviceType.Id})";
            }

            // 直接比较DTO.type与任务中设备类型的ID
            // Arm设备的type范围：1-4 (CF01, CF02, CF03, CF04)
            if (task.ArmType?.DeviceType.Id == dtoType) {
                typeMatches = true;
            }

            // 共享任务场景：如果IP/Port未变，即使类型缺失也不重连
            // 这是因为共享任务中设备类型会在使用时自动初始化
            if (isSharedTask && !needsReconnect) {
                // 共享任务且IP/Port未变，不需要重连
                return false;
            }

            // 独立任务场景：如果类型不匹配，需要重连
            if (!typeMatches) {
                needsReconnect = true;
            }

            if (needsReconnect) {
                MainUtils.Info(_logger, $"Arm[{dto.ip}:{dto.port}] 需要重连 - " +
                    $"IP变化: {task.Ip} -> {dto.ip}, " +
                    $"Port变化: {task.Port} -> {dto.port}, " +
                    $"Type变化: {currentTypeInfo} -> ID={dtoType}" +
                    (isSharedTask ? " [共享任务]" : " [独立任务]"));
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
        /// <summary>
        /// 清理任务（关闭连接）
        /// 注意：缓存移除应由调用者处理（避免重复移除）
        /// </summary>
        private void CleanupTask(IoBoxTask task) {
            try {
                // 只关闭连接，缓存移除由调用者处理
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
        /// 获取所有缓存的任务（使用MainUtils的全局缓存）
        /// </summary>
        public IEnumerable<IoBoxTask> GetAllCachedTasks() {
            return MainUtils.IoBoxTasks.Values.ToList();
        }

        /// <summary>
        /// 查找现有任务（通过key查找）
        /// </summary>
        private IoBoxTask? GetExistingTask(string key) {
            // 通过key快速查找
            if (MainUtils.IoBoxTasks.TryGetValue(key, out var task)) {
                // 检查任务是否被其他设备类型使用
                string deviceType = task.ArmType != null ? "Arm" : "IoBox";
                MainUtils.Info(_logger, $"找到现有任务: {key} ({deviceType}, DeviceId={task.DeviceId})", false);
                return task;
            }

            return null;
        }

        /// <summary>
        /// Find task by device ID - searches both primary DeviceId and DeviceIds list
        /// </summary>
        /// <param name="deviceId">Device ID to search for</param>
        /// <returns>IoBoxTask if found, null otherwise</returns>
        private IoBoxTask? FindTaskByDeviceId(int deviceId) {
            if (deviceId <= 0)
                return null;

            // Use lock to ensure thread safety during search
            lock (MainUtils.IoBoxTasks) {
                return MainUtils.IoBoxTasks.Values.FirstOrDefault(t => t.HasDeviceId(deviceId));
            }
        }

        /// <summary>
        /// 从缓存中移除任务
        /// </summary>
        private void RemoveTaskFromCacheWithMapping(string key, IoBoxTask task) {
            MainUtils.RemoveIoBoxTask(key);
        }

        /// <summary>
        /// Handle device configuration change (IP/Port migration)
        /// </summary>
        /// <param name="oldTask">Task at old IP:Port</param>
        /// <param name="oldKey">Old IP:Port key</param>
        /// <param name="newKey">New IP:Port key</param>
        /// <param name="deviceId">Device ID that changed config</param>
        private void HandleConfigChange(IoBoxTask oldTask, string oldKey, string newKey, int deviceId) {
            // 任何情况都移除当前deviceId
            lock (oldTask) {
                oldTask.RemoveDeviceId(deviceId);
            }

            // 检查是否还有其他设备共享此任务
            bool hasOtherDevices;
            lock (oldTask) {
                hasOtherDevices = oldTask.DeviceIds.Any();
            }
            if (hasOtherDevices) {
                // 有其他设备共享，不关闭task
                int remainingCount;
                lock (oldTask) {
                    remainingCount = oldTask.DeviceIds.Count + (oldTask.DeviceId != deviceId ? 1 : 0);
                }
                MainUtils.Info(_logger, $"Device {deviceId} migrated from {oldKey} to {newKey}, " +
                    $"shared task kept alive with {remainingCount} device(s)", false);
                return;
            }

            // 只有这一个设备在使用，可以安全关闭旧task
            MainUtils.Info(_logger, $"No other devices using task at {oldKey}, safe to close", false);
            RemoveTaskFromCacheWithMapping(oldKey, oldTask);
            CleanupTask(oldTask);
        }
    }
}
