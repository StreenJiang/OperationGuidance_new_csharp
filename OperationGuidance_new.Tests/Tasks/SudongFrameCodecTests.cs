using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SudongFrameCodecTests
{
    private const string StatusFrame = SudongTestFrames.Status;
    private const string AckFrame = SudongTestFrames.Ack;
    private const string ResultFrame = SudongTestFrames.Result;

    [Fact]
    public void Feed_CoalescedStatusAndAck_ReturnsTwoFrames()
    {
        var codec = new SudongFrameCodec();

        var frames = codec.Feed(MainUtils.ToBytes(StatusFrame + AckFrame));

        Assert.Equal(2, frames.Count);
        Assert.Equal(StatusFrame, MainUtils.ToHexString(frames[0]));
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[1]));
    }

    [Fact]
    public void Feed_SplitAck_ReturnsFrameOnceComplete()
    {
        var codec = new SudongFrameCodec();
        byte[] all = MainUtils.ToBytes(AckFrame);

        Assert.Empty(codec.Feed(all.Take(4).ToArray()));
        var frames = codec.Feed(all.Skip(4).ToArray());

        Assert.Single(frames);
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[0]));
    }

    [Fact]
    public void Feed_ThreeFramesInOneChunk_ReturnsAllInOrder()
    {
        var codec = new SudongFrameCodec();

        var frames = codec.Feed(MainUtils.ToBytes(ResultFrame + StatusFrame + AckFrame));

        Assert.Equal(3, frames.Count);
        Assert.Equal(ResultFrame, MainUtils.ToHexString(frames[0]));
        Assert.Equal(StatusFrame, MainUtils.ToHexString(frames[1]));
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[2]));
    }

    [Fact]
    public void Feed_GarbageBeforeHeader_Resyncs()
    {
        var codec = new SudongFrameCodec();
        byte[] input = new byte[] { 0x00, 0x11, 0x22 }
            .Concat(MainUtils.ToBytes(AckFrame)).ToArray();

        var frames = codec.Feed(input);

        Assert.Single(frames);
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[0]));
    }

    [Fact]
    public void Feed_InvalidLengthByte_ResyncsWithoutThrowing()
    {
        var codec = new SudongFrameCodec();
        byte[] input = new byte[] { 0x55, 0xAA, 0xFF, 0xFF, 0xFF }
            .Concat(MainUtils.ToBytes(AckFrame)).ToArray();

        var frames = codec.Feed(input);

        Assert.Single(frames);
        Assert.Equal(AckFrame, MainUtils.ToHexString(frames[0]));
    }

    [Fact]
    public void Reset_DropsPartialBuffer()
    {
        var codec = new SudongFrameCodec();
        byte[] all = MainUtils.ToBytes(AckFrame);
        codec.Feed(all.Take(4).ToArray());

        codec.Reset();
        var frames = codec.Feed(all);

        Assert.Single(frames);
    }

    // ---- 生产安全：真实帧回放（取自 2026-10-14 ~ 10-23 现场日志）----

    [Theory]
    [InlineData(StatusFrame)]
    [InlineData(AckFrame)]
    [InlineData(ResultFrame)]
    [InlineData(SudongTestFrames.Result2)]
    public void Feed_RealSingleFrame_RoundTripsExactly(string captured)
    {
        var codec = new SudongFrameCodec();

        var frames = codec.Feed(MainUtils.ToBytes(captured));

        Assert.Single(frames);
        Assert.Equal(captured, MainUtils.ToHexString(frames[0]));
    }

    [Fact]
    public void Feed_RandomBytes_NeverThrows()
    {
        var random = new Random(12345);
        for (int i = 0; i < 1000; i++) {
            byte[] junk = new byte[random.Next(0, 256)];
            random.NextBytes(junk);
            var codec = new SudongFrameCodec();
            var frames = codec.Feed(junk); // 断言：不抛异常
            Assert.NotNull(frames);
        }
    }

    [Fact]
    public void Feed_OversizedGarbage_ClearsBufferAndCountsDroppedBytes()
    {
        var codec = new SudongFrameCodec();
        byte[] junk = new byte[9 * 1024]; // 无完整帧（全 0x00）

        var frames = codec.Feed(junk); // 断言：不抛异常

        Assert.Empty(frames);
        Assert.True(codec.DroppedBytes > 0, $"DroppedBytes={codec.DroppedBytes}");
    }
}
