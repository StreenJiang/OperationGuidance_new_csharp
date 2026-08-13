using Xunit;

namespace OperationGuidance_new.Tests.Tasks;

public class ToolTaskConnectionSmokeTests
{
    [Fact]
    public async Task ToolTask_Connects_AgainstMockServer()
    {
        using var server = new MockPF6000Server();
        server.Start();
        var task = await ToolTaskTestHarness.ConnectAsync(server);

        Assert.True(task.Connected);
        Assert.True(server.AcceptedConnections >= 1);
        task.CloseConnection();
    }
}
