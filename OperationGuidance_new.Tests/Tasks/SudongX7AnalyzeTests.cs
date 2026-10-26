using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SudongX7AnalyzeTests
{
    private const string AckFrame = SudongTestFrames.Ack;

    [Fact]
    public void AnalyzeSingleFrame_PsetAck_InvokesPsetOkCallback()
    {
        var tool = new ToolSudongX7();
        bool? psetOk = null;
        Action<bool?, bool?, bool?, bool?, bool?> callback =
            (heart, pset, locked, data, curve) => psetOk = pset;

        tool.AnalyzeSingleFrame(MainUtils.ToBytes(AckFrame), callback);

        Assert.True(psetOk);
    }
}
