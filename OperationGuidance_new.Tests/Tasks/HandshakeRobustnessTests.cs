using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class HandshakeRobustnessTests
{
    [Fact]
    public async Task Connect_SucceedsWhenTighteningDataArrivesDuringHandshake()
    {
        using var server = new MockPF6000Server { PushTighteningOnHandshake = true };
        server.Start();

        // 修复前：握手收到 MID 0061 → MID 校验失败 → 无限重试 → 10s 内 CONNECTED 不出现 → TimeoutException（红）
        var task = await ToolTaskTestHarness.ConnectAsync(server);

        Assert.True(task.Connected);
        task.CloseConnection();
    }

    [Fact]
    public async Task Connect_RetriesInsteadOfHanging_WhenNoHandshakeResponse()
    {
        using var server = new MockPF6000Server { SilentHandshake = true };
        server.Start();
        var task = ToolTaskTestHarness.CreateTask(server);
        task.HandshakeTimeoutMs = 500; // 注入小超时，缩短用例时长
        task.Connect();

        // 修复前：握手 ReceiveAsync 无超时 → 永久挂起 → AcceptedConnections 停留为 1（红）
        // 修复后：超时 → Connect 循环 500ms 后重试 → 第二次 accept
        await MockPF6000Server.WaitUntilAsync(() => server.AcceptedConnections >= 2, 3000);

        Assert.True(server.AcceptedConnections >= 2);
        task.CloseConnection();
    }
}
