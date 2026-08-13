using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ReconnectConcurrencyTests
{
    [Fact]
    public async Task ReconnectAndResendPset_NeverSpawnsSecondConcurrentConnect()
    {
        using var server = new MockPF6000Server { SilentHandshake = true };
        server.Start();
        var task = ToolTaskTestHarness.CreateTask(server);
        task.HandshakeTimeoutMs = 500; // 注入小超时，缩短用例时长

        // 后台 Connect 任务进入"握手超时 → 重试"的循环节奏
        task.Connect();
        await MockPF6000Server.WaitUntilAsync(() => server.AcceptedConnections >= 1, 2000);

        // 在后台 Connect 任务存活期间触发重连重试（轮询 10s 必然超时返回 false）
        using var cts = new CancellationTokenSource(15000);
        bool result = await task.ReconnectAndResendPset(5, cts.Token);

        Assert.False(result); // 握手一直被静默，重试轮失败属预期
        // 核心断言：修复前 CloseToTriggerReconnectionAsync 无条件清零 _connectInProgress，
        // 新 Connect 任务与旧任务并发 → 服务端同一时刻存在 2 个活跃连接 → 红。
        // 修复后防重入跳过新任务，只有旧任务循环重试 → 并发恒为 1 → 绿。
        Assert.True(server.MaxConcurrentConnections <= 1,
            $"observed {server.MaxConcurrentConnections} concurrent connections, expected at most 1");
        task.CloseConnection();
    }
}
