using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.AbstractClasses;
using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class SendPSetPreconditionTests
{
    [Fact]
    public async Task SendPSetAsync_RejectsWhenSessionNotReady()
    {
        var task = new TestableToolTask();
        task.SetConnected(true);          // TCP 已连
        task.Status = ATaskBase.CONNECTING; // 但会话未就绪（握手未完成）

        bool sendCalled = false;
        task.SetSendHandler(_ => { sendCalled = true; return true; });

        bool result = await task.SendPSetAsync(5);

        Assert.False(result);
        Assert.False(sendCalled); // 关键断言：不得发送任何命令
    }

    [Fact]
    public async Task SendPSetAsync_RejectsWhenNotConnected()
    {
        var task = new TestableToolTask();
        task.SetConnected(false);
        task.Status = ATaskBase.CONNECTED;

        task.SetSendHandler(_ => true);

        bool result = await task.SendPSetAsync(5);

        Assert.False(result);
    }
}
