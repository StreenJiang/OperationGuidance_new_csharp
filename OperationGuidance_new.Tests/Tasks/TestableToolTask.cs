using System.Reflection;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

/// <summary>
/// Testable ToolTask that overrides SendCommand and Connected for controlled test scenarios.
/// Uses ToolPF6000OP since that's the primary customer-facing tool type.
/// </summary>
public class TestableToolTask : ToolTask
{
    private bool _connected = true;
    private Func<string, bool>? _sendHandler;

    public TestableToolTask(int deviceId = 1, string? name = "TEST", string ip = "127.0.0.1", int port = 4545)
        : base(deviceId, name, ip, port, DeviceType_Tool.PF6000_OP, workstationId: 1)
    {
        // Access private fields via reflection for assertions
    }

    /// <summary>Control the Connected state from tests.</summary>
    public void SetConnected(bool connected) => _connected = connected;
    public override bool Connected => _connected;

    /// <summary>Set a custom handler for SendCommand. Return null to use default (true).</summary>
    public void SetSendHandler(Func<string, bool>? handler) => _sendHandler = handler;

    /// <summary>Simulate SendCommand returns: a list of bool results consumed in order.</summary>
    public void SetSendResults(params bool[] results)
    {
        var queue = new Queue<bool>(results);
        _sendHandler = _ => queue.Count > 0 ? queue.Dequeue() : true;
    }

    protected override bool SendCommand(string command)
    {
        if (_sendHandler != null)
            return _sendHandler(command);
        return true;
    }

    // --- Reflection-based state accessors for assertions ---

    private T GetField<T>(string name) => (T)typeof(ToolTask)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
        .GetValue(this)!;

    private void SetField(string name, object value) => typeof(ToolTask)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
        .SetValue(this, value);

    public bool LockedField => GetField<bool>("_locked");
    public int PendingLockCommandInt
    {
        get
        {
            var f = typeof(ToolTask).GetField("_pendingLockCommand", BindingFlags.NonPublic | BindingFlags.Instance);
            return (int)f!.GetValue(this)!;
        }
    }
    public int SendingPSet => GetField<int>("_sendingPSet");
    public int CurrentPSet => GetField<int>("_currentPSet");
    public bool PSetSentOk => GetField<bool>("_psetSentOk");

    /// <summary>Simulate the tool responding with lock state (as AnalyzeData callback would).</summary>
    public void SimulateLockResponse(bool locked)
    {
        // Call UpdateInternalLockState via reflection
        var method = typeof(ToolTask).GetMethod("UpdateInternalLockState",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(this, new object[] { locked });
    }

    /// <summary>Simulate PSet response from tool (psetSentOk = true).</summary>
    public void SimulatePSetResponse(bool ok)
    {
        SetField("_psetSentOk", ok);
    }

    /// <summary>Get the pending lock command enum name for readable assertions.</summary>
    public string PendingLockCommandName => PendingLockCommandInt switch
    {
        0 => "None",
        1 => "Lock",
        2 => "Unlock",
        _ => $"Unknown({PendingLockCommandInt})"
    };
}
