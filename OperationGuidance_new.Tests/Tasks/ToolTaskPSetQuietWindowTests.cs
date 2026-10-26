using System.Diagnostics;
using OperationGuidance_new.Constants;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ToolTaskPSetQuietWindowTests
{
    private static TestableToolTask CreateX7Task() =>
        new TestableToolTask(DeviceType_Tool.SudongX7);

    [Fact]
    public async Task SendPSetAsync_QuietWindow_DefersSend()
    {
        var t = CreateX7Task();
        t.PSetQuietWindowMs = 400;
        long sendAtMs = -1;
        var stopwatch = Stopwatch.StartNew();
        t.SetSendHandler(_ => {
            sendAtMs = stopwatch.ElapsedMilliseconds;
            return true;
        });
        t.SetLastRundownForTest(t.NowForTest);

        var task = t.SendPSetAsync(2);
        await Task.Delay(150);
        Assert.True(t.SendingPSet == 2, "静默窗口内不应完成发送");
        Assert.Equal(-1, sendAtMs); // 150ms 时仍未真正 SendCommand
        t.SimulatePSetResponse(true);
        bool ok = await task;

        Assert.True(ok);
        Assert.True(sendAtMs >= 350, $"sendAtMs={sendAtMs}, expected >= 350");
    }

    [Fact]
    public async Task SendPSetAsync_WhenDeviceQuiet_SendsImmediately()
    {
        var t = CreateX7Task();
        t.PSetQuietWindowMs = 500;
        long sendAtMs = -1;
        var stopwatch = Stopwatch.StartNew();
        t.SetSendHandler(_ => {
            sendAtMs = stopwatch.ElapsedMilliseconds;
            return true;
        });
        t.SetLastRundownForTest(t.NowForTest - 5000);

        var task = t.SendPSetAsync(2);
        await Task.Delay(30);
        t.SimulatePSetResponse(true);
        await task;

        Assert.True(sendAtMs >= 0 && sendAtMs < 150,
            $"sendAtMs={sendAtMs}, expected < 150");
    }
}
