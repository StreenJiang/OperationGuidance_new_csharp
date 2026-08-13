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
}
