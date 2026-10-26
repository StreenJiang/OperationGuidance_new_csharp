using System.Diagnostics;
using OperationGuidance_new.Constants;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ToolTaskCommandGateTests
{
    [Fact]
    public async Task ForceSendUnlock_And_PSet_AreSerialized_PSetLast()
    {
        var t = new TestableToolTask(DeviceType_Tool.SudongX7);
        // 窗口必须短于解锁序列（约 200ms），否则静默窗口独立就能产生断言顺序、PSet 根本不会竞争闸门
        t.PSetQuietWindowMs = 50;
        var order = new List<string>();

        t.SetSendHandler(cmd => {
            lock (order) {
                order.Add(cmd);
            }
            return true;
        });

        var unlock = Task.Run(() => t.ForceSendUnlock());
        // 等解锁序列真正开始（第一条 unlock 已发出），确保 PSet 一定在序列中间到来
        await MockPF6000Server.WaitUntilAsync(() => {
            lock (order) { return order.Count >= 1; }
        }, 2000);
        var pset = Task.Run(async () => {
            var send = t.SendPSetAsync(2);
            await Task.Delay(20);
            t.SimulatePSetResponse(true);
            return await send;
        });

        await Task.WhenAll(unlock, pset);

        Assert.Equal(3, order.Count);
        Assert.StartsWith("55AA0701", order[0]); // unlock #1
        Assert.StartsWith("55AA0701", order[1]); // unlock #2
        Assert.StartsWith("55AA0702", order[2]); // PSet 必须在最后
        Assert.True(await pset);
    }

    [Fact]
    public async Task SendPSetAsync_AfterLock_StillWaitsForQuietWindow()
    {
        var t = new TestableToolTask(DeviceType_Tool.SudongX7);
        t.PSetQuietWindowMs = 300;
        long lockAt = -1, psetAt = -1;
        var stopwatch = Stopwatch.StartNew();
        t.SetSendHandler(cmd => {
            if (cmd.StartsWith("55AA0701")) lockAt = stopwatch.ElapsedMilliseconds;
            if (cmd.StartsWith("55AA0702")) psetAt = stopwatch.ElapsedMilliseconds;
            return true;
        });

        t.ForceSendLock(); // 两条 lock，最后一条约在 200ms
        var pset = t.SendPSetAsync(2);
        await Task.Delay(30);
        t.SimulatePSetResponse(true);
        await pset;

        Assert.True(psetAt - lockAt >= 280,
            $"lockAt={lockAt}, psetAt={psetAt}, expected gap >= 280");
    }
}
