using System.Reflection;
using OperationGuidance_new.Tasks;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SudongX7DiagnosticsTests
{
    [Fact]
    public async Task ConnectionEpoch_IncrementsAfterReconnect()
    {
        using var server = new MockSudongX7Server();
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        try {
            // ConnectX7Async 返回时后台 Connect() 可能仍在执行 ForceSendUnlock（_connectInProgress == 1），
            // 此时 ReconnectAndResendPset 内部的 Connect() 会被直接跳过，故先等建连任务完全退出。
            await ToolTaskTestHarness.WaitForConnectSettledAsync(task);
            int before = ReadIntField(task, "_connectionEpoch");

            using var cts = new CancellationTokenSource(15000);
            await task.ReconnectAndResendPset(2, cts.Token);

            int after = ReadIntField(task, "_connectionEpoch");
            Assert.True(after > before, $"epoch before={before}, after={after}");
        } finally {
            task.CloseConnection();
        }
    }

    [Fact]
    public async Task StaleAck_WhileIdle_DoesNotMarkPsetSent()
    {
        using var server = new MockSudongX7Server();
        server.Start();
        var task = await ToolTaskTestHarness.ConnectX7Async(server);
        try {
            await server.PushFrameAsync(MockSudongX7Server.PSetOkFrameHex);
            await Task.Delay(150);

            Assert.False(ReadBoolField(task, "_psetSentOk"));
        } finally {
            task.CloseConnection();
        }
    }

    private static int ReadIntField(ToolTask task, string name) =>
        (int) typeof(ToolTask)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(task)!;

    private static bool ReadBoolField(ToolTask task, string name) =>
        (bool) typeof(ToolTask)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(task)!;
}
