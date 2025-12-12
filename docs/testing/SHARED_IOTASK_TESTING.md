# IoBox和Arm共享IoBoxTask架构测试验证

## 测试概述

本文档验证了IoBoxManager中实现的IoBox和Arm设备共享IoBoxTask的支持。

## 关键架构决策

### 1. 共享IoBoxTask机制
- **概念**: IoBox和Arm设备可以使用相同的IP地址和端口
- **实现**: 多个设备类型共享同一个IoBoxTask实例
- **标识**: 使用IP:Port作为缓存键，而非设备ID

### 2. 缓存管理策略
- **键格式**: `MainUtils.GetTCPClientKey(ip, port)` 返回 "ip:port" 格式的键
- **全局缓存**: 使用 `MainUtils.IoBoxTasks` 全局字典
- **设备ID**: 不同设备类型可以有相同的IP:Port但不同的设备ID

## 测试场景

### 场景1: 共享相同IP:Port
```csharp
// IoBox设备
DeviceIoDTO { id=1, ip="192.168.1.100", port=8080, type=Arranger }

// Arm设备（相同IP:Port）
DeviceArmDTO { id=100, ip="192.168.1.100", port=8080, type=CF01 }
```
**预期结果**: 两个DTO共享同一个IoBoxTask实例

### 场景2: 删除Arm设备时保留IoBoxTask
```csharp
// 初始状态：两个设备都存在
IoBox: { id=1, ip="192.168.1.100", port=8080 }
Arm:  { id=100, ip="192.168.1.100", port=8080 }

// 删除Arm设备后
IoBox: { id=1, ip="192.168.1.100", port=8080, deleted=NO }
Arm:  { id=100, ip="192.168.1.100", port=8080, deleted=YES }
```
**预期结果**: IoBoxTask保留，因为IoBox设备仍在使用

### 场景3: 删除所有设备
```csharp
// 两个设备都标记为已删除
IoBox: { id=1, ip="192.168.1.100", port=8080, deleted=YES }
Arm:  { id=100, ip="192.168.1.100", port=8080, deleted=YES }
```
**预期结果**: IoBoxTask被移除

## 关键代码变更

### 1. CleanupTask方法增强
```csharp
private void CleanupTask(IoBoxTask task, bool removeFromCache = true) {
    try {
        // 先从缓存中移除（如果需要）
        if (removeFromCache) {
            string key = MainUtils.GetTCPClientKey(task.Ip, task.Port);
            MainUtils.IoBoxTasks.TryRemove(key, out _);
        }

        // 再关闭连接
        task.CloseConnection();
        MainUtils.Info(_logger, $"任务 {GetDeviceInfo(task)} 已关闭并从缓存中移除");
    } catch (Exception ex) {
        MainUtils.Warn(_logger, $"关闭任务 {GetDeviceInfo(task)} 时出错: {ex.Message}");
    }
}
```
**目的**: 整合缓存清除逻辑，支持选择性缓存移除

### 2. RemoveDeletedDevices重新设计
```csharp
public void RemoveDeletedDevices(
    IEnumerable<DeviceIoDTO> activeIoBoxDtos,
    IEnumerable<DeviceArmDTO> activeArmDtos) {
    try {
        // 构建活跃的IP:Port键集合
        var activeIoBoxKeys = new HashSet<string>();
        var activeArmKeys = new HashSet<string>();

        foreach (var dto in activeIoBoxDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
            activeIoBoxKeys.Add(MainUtils.GetTCPClientKey(dto.ip, dto.port));
        }

        foreach (var dto in activeArmDtos.Where(d => d.deleted == (int) YesOrNo.NO)) {
            activeArmKeys.Add(MainUtils.GetTCPClientKey(dto.ip, dto.port));
        }

        // 移除不再活跃的IoBoxTask（基于键判断）
        var tasksToRemove = new List<KeyValuePair<string, IoBoxTask>>();
        foreach (var kvp in MainUtils.IoBoxTasks) {
            var task = kvp.Value;
            var key = kvp.Key;

            // 检查是否被任何活跃设备使用
            bool isUsedByIoBox = activeIoBoxKeys.Contains(key);
            bool isUsedByArm = activeArmKeys.Contains(key);

            // 只有在完全未被使用时才移除
            if (!isUsedByIoBox && !isUsedByArm) {
                tasksToRemove.Add(kvp);
            }
        }

        // 执行删除
        foreach (var kvp in tasksToRemove) {
            if (MainUtils.IoBoxTasks.TryRemove(kvp.Key, out var task)) {
                CleanupTask(task, removeFromCache: false);
                // ...
            }
        }
    } catch (Exception ex) {
        MainUtils.Error(_logger, $"移除已删除的IoBox/Arm设备时出错: {ex.Message}");
    }
}
```
**目的**: 使用键判断而非设备ID分离，支持共享IoBoxTask架构

### 3. 设备查找增强
```csharp
private IoBoxTask? GetExistingTask(string key) {
    // 先通过key快速查找
    if (MainUtils.IoBoxTasks.TryGetValue(key, out var task)) {
        return task;
    }

    // 如果key查找失败，尝试通过设备ID查找
    if (TryGetDeviceIdFromKey(key, out int deviceId)) {
        task = MainUtils.IoBoxTasks.Values.FirstOrDefault(t => t.DeviceId == deviceId);
        if (task != null) {
            MainUtils.Info(_logger, $"通过设备ID找到任务: {task.Ip}:{task.Port} (DeviceId={deviceId})", false);
            return task;
        }
    }

    return null;
}
```
**目的**: 支持配置变更场景（IP/Port改变）

## 测试文件

`IoBoxManager_SharedTaskTests.cs` 包含以下测试：
1. `Test_SharedIoBoxTask_SameIpPort` - 验证共享IoBoxTask
2. `Test_DeleteArm_KeepIoBoxTask` - 验证部分删除场景
3. `Test_DeleteAll_RemoveIoBoxTask` - 验证完全删除场景
4. `Test_ConfigurationChange` - 验证配置变更场景

## 运行测试

```csharp
var test = new IoBoxManager_SharedTaskTests();
test.RunAllTests();
```

## 预期结果

✓ 所有测试通过
✓ 编译无错误
✓ 支持IoBox和Arm共享IoBoxTask
✓ 正确处理设备删除逻辑
✓ 正确处理配置变更场景

## 注意事项

1. **设备ID空间**: IoBox和Arm设备的ID空间是独立的，不会冲突
2. **IP:Port共享**: 允许IoBox和Arm设备使用相同的IP:Port
3. **缓存键**: 使用IP:Port作为缓存键，支持设备类型共存
4. **删除逻辑**: 只有在设备完全未被使用时才移除IoBoxTask
