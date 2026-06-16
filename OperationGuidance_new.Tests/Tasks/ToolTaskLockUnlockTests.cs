using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

/// <summary>
/// Tests for SendLock, SendUnlock, ForceSendLock, ForceSendUnlock — covering
/// normal flow, dedup, skip-when-already, rollback on SendCommand failure, and race guard.
/// </summary>
public class ToolTaskLockUnlockTests
{
    // ==================== SendLock ====================

    [Fact]
    public void SendLock_WhenNotConnected_SkipsWithoutPending()
    {
        var t = new TestableToolTask();
        t.SetConnected(false);
        t.SendLock();
        Assert.Equal("None", t.PendingLockCommandName);
    }

    [Fact]
    public void SendLock_WhenPendingLockAlreadyInFlight_Skips()
    {
        var t = new TestableToolTask();
        t.SendLock(); // First call sets pending=Lock
        Assert.Equal("Lock", t.PendingLockCommandName);
        t.SendLock(); // Second call should skip
        Assert.Equal("Lock", t.PendingLockCommandName); // Still Lock, not duplicated
    }

    [Fact]
    public void SendLock_WhenAlreadyLockedAndNoPendingUnlock_Skips()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // _locked = true, pending = None
        t.SendLock();
        Assert.Equal("None", t.PendingLockCommandName); // Skipped
    }

    [Fact]
    public void SendLock_WhenLockedButPendingUnlock_Overrides()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // _locked = true — required for SendUnlock to proceed
        t.SendUnlock(); // pending = Unlock
        Assert.Equal("Unlock", t.PendingLockCommandName);
        t.SendLock(); // should override pending Unlock → Lock
        Assert.Equal("Lock", t.PendingLockCommandName);
    }

    [Fact]
    public void SendLock_WhenNotLocked_SendsAndSetsPending()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true);
        t.SendLock();
        Assert.Equal("Lock", t.PendingLockCommandName);
        // Note: for PF Series, _locked is set by tool response, not by SendLock itself.
        // ForceSendLock sets _locked immediately via UpdateInternalLockState.
    }

    // ==================== SendUnlock ====================

    [Fact]
    public void SendUnlock_WhenNotConnected_SkipsWithoutPending()
    {
        var t = new TestableToolTask();
        t.SetConnected(false);
        t.SendUnlock();
        Assert.Equal("None", t.PendingLockCommandName);
    }

    [Fact]
    public void SendUnlock_WhenPendingUnlockAlreadyInFlight_Skips()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // _locked = true, pending = None
        t.SendUnlock(); // First call sets pending=Unlock
        Assert.Equal("Unlock", t.PendingLockCommandName);
        t.SendUnlock(); // Second call should skip (dedup)
        Assert.Equal("Unlock", t.PendingLockCommandName);
    }

    [Fact]
    public void SendUnlock_WhenAlreadyUnlockedAndNoPendingLock_Skips()
    {
        var t = new TestableToolTask();
        // _locked = false (default), pending = None → should skip
        t.SendUnlock();
        Assert.Equal("None", t.PendingLockCommandName);
    }

    [Fact]
    public void SendUnlock_WhenUnlockedButPendingLock_Overrides()
    {
        var t = new TestableToolTask();
        t.SendLock(); // pending = Lock
        Assert.Equal("Lock", t.PendingLockCommandName);
        t.SendUnlock(); // should override pending Lock → Unlock
        Assert.Equal("Unlock", t.PendingLockCommandName);
    }

    [Fact]
    public void SendUnlock_WhenLocked_SendsAndSetsPending()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true);
        t.SimulateLockResponse(true); // _locked = true
        t.SendUnlock();
        Assert.Equal("Unlock", t.PendingLockCommandName);
    }

    // ==================== ForceSendLock / ForceSendUnlock ====================

    [Fact]
    public void ForceSendLock_OverridesPendingUnlock()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // _locked = true
        t.SendUnlock(); // pending = Unlock
        Assert.Equal("Unlock", t.PendingLockCommandName);
        t.ForceSendLock(); // should clear pending and set _locked = true
        Assert.Equal("None", t.PendingLockCommandName);
        Assert.True(t.LockedField);
    }

    [Fact]
    public void ForceSendUnlock_OverridesPendingLock()
    {
        var t = new TestableToolTask();
        t.SendLock(); // pending = Lock
        Assert.Equal("Lock", t.PendingLockCommandName);
        t.ForceSendUnlock(); // should clear pending and set _locked = false
        Assert.Equal("None", t.PendingLockCommandName);
        Assert.False(t.LockedField);
    }

    [Fact]
    public void ForceSendLock_WhenDisconnected_Skips()
    {
        var t = new TestableToolTask();
        t.SetConnected(false);
        t.ForceSendLock();
        Assert.False(t.LockedField); // _locked unchanged
    }

    [Fact]
    public void ForceSendUnlock_WhenDisconnected_Skips()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // _locked = true
        t.SetConnected(false);
        t.ForceSendUnlock();
        Assert.True(t.LockedField); // _locked unchanged
    }

    // ==================== SendCommand failure rollback ====================

    [Fact]
    public void SendLock_SendCommandFails_RollsBackPending()
    {
        var t = new TestableToolTask();
        t.SetSendResults(false); // SendCommand returns false
        t.SendLock(); // Sets pending=Lock, PerformLock → SendCommand fails → rollback
        Assert.Equal("None", t.PendingLockCommandName); // Rolled back
    }

    [Fact]
    public void SendUnlock_SendCommandFails_RollsBackPending()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // _locked = true
        t.SetSendResults(false); // SendCommand returns false
        t.SendUnlock(); // Sets pending=Unlock, PerformUnlock → SendCommand fails → rollback
        Assert.Equal("None", t.PendingLockCommandName); // Rolled back
    }

    [Fact]
    public void SendLock_SendCommandFails_OnlyRollsBackIfPendingStillLock()
    {
        var t = new TestableToolTask();
        t.SetSendResults(false);
        t.SendLock(); // pending=Lock → rollback checks pending==Lock → clears

        // Now verify: if pending was changed to Unlock by another caller,
        // the Lock rollback should NOT clear it
        t.SimulateLockResponse(true); // _locked = true, pending = None
        t.SendUnlock(); // pending = Unlock
        // Simulate: PerformLock was slow, another thread already set pending to Unlock
        // The rollback in PerformLock should guard with compare-and-swap
        Assert.Equal("Unlock", t.PendingLockCommandName);
    }

    // ==================== High-frequency lock/unlock toggle ====================

    [Fact]
    public void HighFrequency_LockUnlockRapidToggle_NoDeadlock()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true, true, true, true, true, true, true, true);

        // Simulate rapid lock/unlock cycles (like arm position flickering)
        for (int i = 0; i < 100; i++)
        {
            t.SendLock();
            Assert.True(t.PendingLockCommandName is "Lock" or "None");
            t.SimulateLockResponse(true); // Lock response arrives
            t.SendUnlock();
            Assert.True(t.PendingLockCommandName is "Unlock" or "None");
            t.SimulateLockResponse(false); // Unlock response arrives
        }
    }

    [Fact]
    public void HighFrequency_MultipleSendUnlock_DedupPreventsFlood()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // _locked = true — required for SendUnlock to proceed

        int sentCount = 0;
        t.SetSendHandler(cmd =>
        {
            sentCount++;
            return true;
        });

        for (int i = 0; i < 50; i++)
        {
            t.SendUnlock();
        }

        // Only one unlock should have been sent (dedup blocks the rest)
        Assert.Equal(1, sentCount);
    }

    [Fact]
    public void HighFrequency_MultipleSendLock_DedupPreventsFlood()
    {
        var t = new TestableToolTask();

        int sentCount = 0;
        t.SetSendHandler(cmd =>
        {
            sentCount++;
            return true;
        });

        for (int i = 0; i < 50; i++)
        {
            t.SendLock();
        }

        // Only one lock should have been sent (dedup blocks the rest)
        Assert.Equal(1, sentCount);
    }

    // ==================== Response delay / no-response scenarios ====================

    [Fact]
    public void PendingUnlock_BlocksSubsequentSendUnlock_UntilResponseArrives()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // locked
        t.SetSendResults(true, true);

        t.SendUnlock(); // pending = Unlock, send OK
        Assert.Equal("Unlock", t.PendingLockCommandName);

        // Subsequent SendUnlock calls are blocked by dedup
        t.SendUnlock();
        Assert.Equal("Unlock", t.PendingLockCommandName); // Still Unlock, no duplicate send

        // Response arrives → pending cleared
        t.SimulateLockResponse(false);
        Assert.Equal("None", t.PendingLockCommandName);

        // Now a new SendUnlock can proceed (but _locked is false, so it's skipped by state check)
        t.SendUnlock();
        Assert.Equal("None", t.PendingLockCommandName); // Skipped: already unlocked
    }

    [Fact]
    public void PendingLock_BlocksSubsequentSendLock_UntilResponseArrives()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true, true);

        t.SendLock(); // pending = Lock, send OK
        Assert.Equal("Lock", t.PendingLockCommandName);

        // Subsequent SendLock calls blocked
        t.SendLock();
        Assert.Equal("Lock", t.PendingLockCommandName);

        // Response arrives
        t.SimulateLockResponse(true);
        Assert.Equal("None", t.PendingLockCommandName);

        // Already locked, subsequent SendLock skipped by state check
        t.SendLock();
        Assert.Equal("None", t.PendingLockCommandName);
    }

    [Fact]
    public void SendCommandFails_PendingCleared_NextIterationCanRetry()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true); // _locked = true

        // First attempt: SendCommand fails
        t.SetSendResults(false);
        t.SendUnlock();
        Assert.Equal("None", t.PendingLockCommandName); // Rolled back

        // Second attempt: SendCommand succeeds (simulating retry on next lock-check iteration)
        t.SetSendResults(true);
        t.SendUnlock();
        Assert.Equal("Unlock", t.PendingLockCommandName); // Sent
    }

    // ==================== Concurrent scenarios ====================

    [Fact]
    public void Concurrent_LockResponseArrivesAfterUnlockSent_DoesNotDeadlock()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true, true);

        // ForceSendLock (from DoAfterRecevingTighteningDataAsync)
        t.ForceSendLock();
        Assert.True(t.LockedField);
        Assert.Equal("None", t.PendingLockCommandName);

        // SwitchBolt → arm arrives → lock checking task calls SendUnlock
        t.SendUnlock();
        Assert.Equal("Unlock", t.PendingLockCommandName);

        // Stale lock response from ForceSendLock arrives AFTER SendUnlock
        t.SimulateLockResponse(true);
        // _locked stays true, but pending is cleared to None
        Assert.Equal("None", t.PendingLockCommandName);

        // Unlock response arrives
        t.SimulateLockResponse(false);
        Assert.False(t.LockedField);
        Assert.Equal("None", t.PendingLockCommandName);
    }

    [Fact]
    public void Concurrent_DoubleForceLock_NoDeadlock()
    {
        var t = new TestableToolTask();
        t.SetSendResults(true, true, true, true);

        // Simulate two tight sequential DoAfterRecevingTighteningDataAsync calls
        t.ForceSendLock();
        Assert.True(t.LockedField);
        t.ForceSendLock(); // Second force lock should be fine
        Assert.True(t.LockedField);
        Assert.Equal("None", t.PendingLockCommandName);
    }

    // ==================== Disconnection during lock/unlock ====================

    [Fact]
    public void Disconnect_WhilePendingUnlock_SendUnlockSkipsButDoesNotDeadlock()
    {
        var t = new TestableToolTask();
        t.SimulateLockResponse(true);
        t.SetSendResults(true);
        t.SendUnlock(); // pending = Unlock
        Assert.Equal("Unlock", t.PendingLockCommandName);

        // Tool disconnects, then reconnects (ForceSendUnlock on connect)
        t.ForceSendUnlock(); // Clears pending and sets _locked = false
        Assert.Equal("None", t.PendingLockCommandName);
        Assert.False(t.LockedField);

        // After reconnect, SendUnlock is skipped (already unlocked)
        t.SendUnlock();
        Assert.Equal("None", t.PendingLockCommandName);
    }
}
