using System.Reflection;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.AbstractClasses;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

/// <summary>集成测试脚手架：起服务 → 建 ToolTask → Connect → 轮询等待会话就绪。</summary>
public static class ToolTaskTestHarness
{
    /// <summary>创建指向 mock 服务器的 ToolTask（不发起连接）。</summary>
    public static ToolTask CreateTask(MockPF6000Server server)
    {
        return new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.PF6000_OP, 1);
    }

    /// <summary>建 Task 并 Connect，轮询等待会话就绪（Status == CONNECTED）后返回。</summary>
    public static async Task<ToolTask> ConnectAsync(MockPF6000Server server, int timeoutMs = 10000)
    {
        var task = CreateTask(server);
        task.Connect(); // fire-and-forget；ConnectAsync 只是同步包装，必须轮询等待
        await MockPF6000Server.WaitUntilAsync(() => task.Status == ATaskBase.CONNECTED, timeoutMs);
        return task;
    }

    /// <summary>创建指向 mock X7 控制器的 ToolTask（不发起连接）。</summary>
    public static ToolTask CreateX7Task(MockSudongX7Server server)
    {
        return new ToolTask(1, "TEST", "127.0.0.1", server.Port, DeviceType_Tool.SudongX7, 1);
    }

    /// <summary>建 Task 并 Connect，轮询等待会话就绪后返回。</summary>
    public static async Task<ToolTask> ConnectX7Async(MockSudongX7Server server, int timeoutMs = 10000)
    {
        var task = CreateX7Task(server);
        task.Connect();
        await MockPF6000Server.WaitUntilAsync(() => task.Status == ATaskBase.CONNECTED, timeoutMs);
        return task;
    }

    /// <summary>等待 Connect() 的后台任务完全退出（含 ForceSendUnlock），避免紧接着调用 Connect() 被早退跳过。</summary>
    public static Task WaitForConnectSettledAsync(ToolTask task, int timeoutMs = 5000)
    {
        var field = typeof(ToolTask).GetField("_connectInProgress", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return MockPF6000Server.WaitUntilAsync(() => (int)field.GetValue(task)! == 0, timeoutMs);
    }

    /// <summary>等待客户端记录到一条拧紧结果帧（判据：时间戳不再是 NeverMs 哨兵）。</summary>
    public static Task WaitForRundownNotedAsync(ToolTask task, int timeoutMs = 2000)
    {
        var field = typeof(ToolTask).GetField("_lastRundownAtMs", BindingFlags.NonPublic | BindingFlags.Instance)!;
        long neverMs = (long) typeof(ToolTask)
            .GetField("NeverMs", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        return MockPF6000Server.WaitUntilAsync(() => (long)field.GetValue(task)! != neverMs, timeoutMs);
    }
}
