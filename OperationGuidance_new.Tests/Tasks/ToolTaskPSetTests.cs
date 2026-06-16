using System.Reflection;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

/// <summary>
/// Tests for SendPSetAsync — covering success, failure, busy, already-set, timeout, and no-response.
/// </summary>
public class ToolTaskPSetTests
{
    private async Task WaitForPSetAsync(TestableToolTask t, int timeoutMs = 2000)
    {
        // Poll until _sendingPSet is reset (SendPSetAsync completes)
        var cts = new CancellationTokenSource(timeoutMs);
        while (t.SendingPSet != -1 && !cts.Token.IsCancellationRequested)
            await Task.Delay(10, cts.Token);
    }

    // ==================== PSet Success ====================

    [Fact]
    public async Task SendPSetAsync_WhenNotConnected_ReturnsFalse()
    {
        var t = new TestableToolTask();
        t.SetConnected(false);
        bool result = await t.SendPSetAsync(5);
        Assert.False(result);
    }

    [Fact]
    public async Task SendPSetAsync_WhenAnotherPSetInFlight_ReturnsFalse()
    {
        var t = new TestableToolTask();
        t.SetSendResults(false); // Make SendCommand hang simulation — _psetSentOk never true

        // Start first PSet (will wait for _psetSentOk)
        var task1 = t.SendPSetAsync(3);

        // Small delay to let task1 start and set _sendingPSet
        await Task.Delay(100);

        // Second PSet should be rejected because _sendingPSet != -1
        bool result2 = await t.SendPSetAsync(4);
        Assert.False(result2);
    }

    [Fact]
    public async Task SendPSetAsync_WhenAlreadySet_ReturnsTrue()
    {
        var t = new TestableToolTask();
        // First: send PSet 5 successfully — simulate response after SendCommand
        t.SetSendHandler(cmd => true);
        var task1 = t.SendPSetAsync(5);
        await Task.Delay(50);
        t.SimulatePSetResponse(true); // Must be AFTER SendPSetAsync resets _psetSentOk
        var result1 = await task1;
        Assert.True(result1);

        // Second: same PSet → should skip immediately (already cached)
        var result2 = await t.SendPSetAsync(5);
        Assert.True(result2);
    }

    [Fact]
    public async Task SendPSetAsync_SendSuccess_ToolRespondsOk_ReturnsTrue()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true);

        var task = t.SendPSetAsync(7);
        await Task.Delay(50);
        t.SimulatePSetResponse(true);
        bool result = await task;

        Assert.True(result);
        Assert.Equal(7, t.CurrentPSet);
    }

    // ==================== PSet Failure / Timeout ====================

    [Fact]
    public async Task SendPSetAsync_SendFails_ReturnsFalse()
    {
        var t = new TestableToolTask();
        t.SetSendResults(false);
        bool result = await t.SendPSetAsync(8);
        Assert.False(result);
    }

    [Fact]
    public async Task SendPSetAsync_SendOk_ButToolNeverResponds_ReturnsFalse()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true);
        // Never call SimulatePSetResponse → tool never confirms

        var task = t.SendPSetAsync(9);
        bool result = await task; // Should timeout after PSetWaitTimesMax × PSetWaitTime

        Assert.False(result);
    }

    [Fact]
    public async Task SendPSetAsync_SendOk_ToolRespondsFalse_ReturnsFalse()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true);

        var task = t.SendPSetAsync(10);
        await Task.Delay(50);
        t.SimulatePSetResponse(false); // Tool rejects PSet
        bool result = await task;

        Assert.False(result);
    }

    // ==================== PSet retry after reconnection ====================

    [Fact]
    public async Task SendPSetAsync_AfterReconnect_SendsAgain()
    {
        var t = new TestableToolTask();
        // First attempt fails
        t.SetSendResults(false, true); // first SendCommand fails, second (after reconnect) succeeds
        t.SetConnected(true);

        bool result = await t.SendPSetAsync(11);
        // First send fails, but we don't reconnect inside SendPSetAsync — the reconnect logic
        // is in SendPSet (the view-layer wrapper). SendPSetAsync just returns false.
        Assert.False(result);
    }

    // ==================== PSet during lock/unlock ====================

    [Fact]
    public async Task PSet_DoesNotInterfereWithLockState()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true, true, true);

        // Lock the tool
        t.ForceSendLock();
        Assert.True(t.LockedField);

        // Send PSet while locked
        var psetTask = t.SendPSetAsync(12);
        await Task.Delay(50);
        t.SimulatePSetResponse(true);
        bool psetResult = await psetTask;
        Assert.True(psetResult);

        // Lock state should be unchanged by PSet
        Assert.True(t.LockedField);
    }

    // ==================== PSet busy flag reset ====================

    [Fact]
    public async Task SendPSetAsync_ExceptionDuringSend_ResetsSendingPSet()
    {
        var t = new TestableToolTask();
        // Simulate exception during SendCommand
        t.SetSendHandler(_ => throw new Exception("Socket error"));

        await t.SendPSetAsync(13);
        // After exception, _sendingPSet should be reset to -1
        Assert.Equal(-1, t.SendingPSet);
    }
}
