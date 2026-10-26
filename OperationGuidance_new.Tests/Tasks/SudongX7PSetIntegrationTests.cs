using System.Diagnostics;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SudongX7PSetIntegrationTests
{
    [Fact]
    public async Task SendPSetAsync_ImmediatelyAfterRundown_ReproducesTimeout()
    {
        using var server = new MockSudongX7Server { BusyAfterRundownMs = 1000 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 0; // 关闭静默窗口，复现现场「收到结果后立即下发」

        await server.PushRundownResultAsync();
        bool ok = await task.SendPSetAsync(2);

        Assert.False(ok);
        Assert.Equal(1, server.IgnoredPSetCount);
        task.CloseConnection();
    }

    [Fact]
    public async Task SendPSetAsync_CoalescedStatusAndAck_ReturnsTrue()
    {
        using var server = new MockSudongX7Server { CoalesceAckWithStatus = true };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 0;

        bool ok = await task.SendPSetAsync(2);

        Assert.True(ok);
        task.CloseConnection();
    }

    [Fact]
    public async Task SendPSetAsync_AfterRundown_WithQuietWindow_Succeeds()
    {
        using var server = new MockSudongX7Server { BusyAfterRundownMs = 600 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 800;

        await server.PushRundownResultAsync();
        // 等待客户端真正记录到结果帧时间，避免在静默窗口尚未生效前就下发
        await ToolTaskTestHarness.WaitForRundownNotedAsync(task);
        var stopwatch = Stopwatch.StartNew();
        bool ok = await task.SendPSetAsync(2);
        stopwatch.Stop();

        Assert.True(ok);
        Assert.True(stopwatch.ElapsedMilliseconds >= 700,
            $"elapsed={stopwatch.ElapsedMilliseconds}, expected >= 700");
        Assert.Single(server.AcceptedPSetTimes);
        task.CloseConnection();
    }

    [Fact]
    public async Task ReconnectAndResendPset_AfterBusyWindow_Succeeds()
    {
        using var server = new MockSudongX7Server { BusyAfterRundownMs = 600 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        // Connect() 建连后还会在后台执行 ForceSendUnlock()，期间 _connectInProgress 仍为 1，
        // 若此时调用 Connect()（ReconnectAndResendPset 内部）会被直接跳过。等它完全退出再重连。
        await ToolTaskTestHarness.WaitForConnectSettledAsync(task);
        task.PSetQuietWindowMs = 800;

        await server.PushRundownResultAsync(); // 设备刚发完结果，处于忙碌窗口

        using var cts = new CancellationTokenSource(15000);
        bool ok = await task.ReconnectAndResendPset(2, cts.Token);

        Assert.True(ok);
        Assert.True(server.AcceptedConnections >= 2, "expected a reconnect");
        task.CloseConnection();
    }

    // 注意：BusyAfterCommandMs 是「锁/解锁后忙碌窗口」的推断值，仅用于锁住修复逻辑，
    // 不是设备实测规格。真实值由下一版现场日志校准。

    [Fact]
    public async Task SendPSetAsync_RightAfterLock_WithoutQuietWindow_IsIgnored()
    {
        using var server = new MockSudongX7Server { BusyAfterCommandMs = 300 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 0; // 关闭静默窗口，复现「紧贴锁命令下发」

        task.ForceSendLock();       // 两条 lock，约 200ms
        bool ok = await task.SendPSetAsync(2);

        Assert.False(ok);
        Assert.True(server.IgnoredPSetCount >= 1);
        task.CloseConnection();
    }

    [Fact]
    public async Task SendPSetAsync_RightAfterLock_WithQuietWindow_IsAccepted()
    {
        using var server = new MockSudongX7Server { BusyAfterCommandMs = 300 };
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        task.PSetQuietWindowMs = 400;

        task.ForceSendLock();
        bool ok = await task.SendPSetAsync(2);

        Assert.True(ok);
        Assert.Single(server.AcceptedPSetTimes);
        task.CloseConnection();
    }
}
