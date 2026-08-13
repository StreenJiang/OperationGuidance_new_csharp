using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ToolTaskReconnectIntegrationTests
{
    [Fact]
    public async Task ReconnectAndResendPset_SendsPSetOnlyAfterHandshake()
    {
        using var server = new MockPF6000Server { HandshakeDelayMs = 400 }; // 真实控制器节奏：3×400ms 握手 ≈ 1.2s > 200ms 轮询粒度
        server.Start();
        var task = await ToolTaskTestHarness.ConnectAsync(server);
        server.ResetTimeline();

        // 触发断连+重连+重发
        using var cts = new CancellationTokenSource(20000);
        bool result = await task.ReconnectAndResendPset(5, cts.Token);

        Assert.True(result,
            $"ReconnectAndResendPset returned false; PSet arrived at {server.PSetCommandArrivalUtc:O}, handshake completed at {server.LastHandshakeResponseSentUtc:O}");
        // 核心时序断言：PSet 命令到达服务端的时刻，必须晚于本轮握手最后一条响应发出的时刻
        Assert.NotNull(server.PSetCommandArrivalUtc);
        Assert.NotNull(server.LastHandshakeResponseSentUtc);
        Assert.True(server.PSetCommandArrivalUtc >= server.LastHandshakeResponseSentUtc,
            $"PSet arrived at {server.PSetCommandArrivalUtc:O} before handshake completed at {server.LastHandshakeResponseSentUtc:O}");
        task.CloseConnection();
    }

    /// <summary>
    /// 复现现场日志 20:14 失败集群的完整链路：会话失活（TCP 保持、命令无响应）
    /// → PSet 超时 → 重连重试 → 新会话恢复 → 下发成功。证明"无需重新激活任务即可自动恢复"。
    /// </summary>
    [Fact]
    public async Task PsetFailureRecoversViaReconnect_EndToEnd()
    {
        using var server = new MockPF6000Server();
        server.Start();
        var task = await ToolTaskTestHarness.ConnectAsync(server);

        // 模拟控制器会话失活：TCP 保持，但所有命令石沉大海
        server.SilentMode = true;
        bool first = await task.SendPSetAsync(5);
        Assert.False(first); // 1000ms 等待窗口内无 ACK，超时失败（环节 1 的固有检测手段）

        // 控制器恢复，触发断连+重连+重发
        server.SilentMode = false;
        using var cts = new CancellationTokenSource(20000);
        bool recovered = await task.ReconnectAndResendPset(5, cts.Token);

        // 修复前：重连轮 PSet 抢跑，recovered == false（红）
        // 修复后：会话就绪后才发送，ACK 被 RunTask 解析，recovered == true（绿）
        Assert.True(recovered);
        Assert.NotNull(server.PSetCommandArrivalUtc);
        Assert.NotNull(server.LastHandshakeResponseSentUtc);
        Assert.True(server.PSetCommandArrivalUtc >= server.LastHandshakeResponseSentUtc);
        task.CloseConnection();
    }
}
