using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OperationGuidance_new.Tests.Tasks;

/// <summary>
/// 模拟 Atlas PF6000 Open Protocol 控制器（TCP 服务端）。
/// 命令/响应格式：长度头(4位十进制, =总字符数-1, 不含尾部 \x00) + MID(4位) + 数据 + \x00。
/// 客户端无分帧逻辑，每条响应必须独立写入并间隔 ≥20ms，避免被客户端一次 Receive 合并。
/// </summary>
public sealed class MockPF6000Server : IDisposable
{
    private const int InterMessageDelayMs = 20;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private Task? _acceptLoop;
    private int _activeConnections;

    public int Port { get; private set; }

    /// <summary>不回任何握手响应（用于握手超时测试）。</summary>
    public bool SilentHandshake { get; set; }

    /// <summary>收到 connect 命令后、回握手响应前，先推送一条 MID 0061 拧紧数据报文。</summary>
    public bool PushTighteningOnHandshake { get; set; }

    /// <summary>会话建立后不响应任何命令，但保持 TCP 连接（模拟控制器会话失活）。</summary>
    public bool SilentMode { get; set; }

    /// <summary>收到握手命令后、回握手响应前的延迟（毫秒），模拟现场控制器响应节奏（现场握手 300-470ms）。</summary>
    public int HandshakeDelayMs { get; set; } = InterMessageDelayMs;

    public int AcceptedConnections { get; private set; }
    public int MaxConcurrentConnections { get; private set; }
    public DateTime? LastHandshakeResponseSentUtc { get; private set; }
    public DateTime? PSetCommandArrivalUtc { get; private set; }

    public MockPF6000Server()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0); // 随机可用端口
    }

    public void Start()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    public void ResetTimeline()
    {
        lock (_lock)
        {
            LastHandshakeResponseSentUtc = null;
            PSetCommandArrivalUtc = null;
        }
    }

    public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs)
                throw new TimeoutException($"condition not met within {timeoutMs}ms");
            await Task.Delay(50);
        }
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            lock (_lock)
            {
                _activeConnections++;
                AcceptedConnections++;
                MaxConcurrentConnections = Math.Max(MaxConcurrentConnections, _activeConnections);
            }
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                var pending = new List<byte>();
                while (!_cts.IsCancellationRequested)
                {
                    int n = await stream.ReadAsync(buffer, _cts.Token);
                    if (n <= 0) break; // 客户端断开
                    pending.AddRange(buffer.Take(n));
                    int idx;
                    while ((idx = pending.IndexOf(0)) >= 0)
                    {
                        string cmd = Encoding.ASCII.GetString(pending.Take(idx).ToArray());
                        pending.RemoveRange(0, idx + 1);
                        await HandleCommandAsync(stream, cmd);
                    }
                }
            }
        }
        catch
        {
            // 客户端断开或 Dispose 取消，正常退出
        }
        finally
        {
            lock (_lock) { _activeConnections--; }
        }
    }

    private async Task HandleCommandAsync(NetworkStream stream, string cmd)
    {
        string mid = cmd.Length >= 8 ? cmd.Substring(4, 4) : "";
        if (SilentMode) return;

        switch (mid)
        {
            case "0001": // connect 命令
                if (PushTighteningOnHandshake)
                {
                    await WriteAsync(stream, BuildResponse("0061", 50)); // 拧紧数据（握手线程应过滤掉），保持原延迟
                }
                await RespondToHandshakeAsync(stream, "0002", 221); // 222 字节，与现场日志一致
                break;
            case "0060": // data subscribe 命令（MID 0060 = Last tightening result data subscribe，与生产 COMMAND_DATA_ASCII 一致）
                await RespondToHandshakeAsync(stream, "0005", 25);
                break;
            case "0008": // curve enable 命令（客户端不校验 MID）
                await RespondToHandshakeAsync(stream, "0005", 27);
                lock (_lock) { LastHandshakeResponseSentUtc = DateTime.UtcNow; }
                break;
            case "0018": // PSet 命令
                lock (_lock) { PSetCommandArrivalUtc = DateTime.UtcNow; }
                await WriteAckAsync(stream, "0018");
                break;
            case "0042": // lock 命令 → lock ACK（tail 0042）
                await WriteAckAsync(stream, "0042");
                break;
            case "0043": // unlock 命令 → unlock ACK（tail 0043）
                await WriteAckAsync(stream, "0043");
                break;
            default:
                break; // 心跳(MID 9999)等不响应
        }
    }

    /// <summary>回一条握手响应：静默模式跳过；否则先等 HandshakeDelayMs（模拟现场节奏）再写入。</summary>
    private async Task RespondToHandshakeAsync(NetworkStream stream, string mid, int totalChars)
    {
        if (SilentHandshake) return;
        await Task.Delay(HandshakeDelayMs, _cts.Token);
        await WriteAsync(stream, BuildResponse(mid, totalChars));
    }

    /// <summary>回一条 MID 0005 命令 ACK，tail 回显命令号（生产 GetTail 取末 4 位判定）。</summary>
    private async Task WriteAckAsync(NetworkStream stream, string tail)
    {
        await WriteAsync(stream, "0025" + "0005" + "001" + new string('0', 10) + tail + "\x00");
    }

    private async Task WriteAsync(NetworkStream stream, string payload)
    {
        await stream.WriteAsync(Encoding.ASCII.GetBytes(payload), _cts.Token);
        await Task.Delay(InterMessageDelayMs, _cts.Token); // 保证分段
    }

    /// <summary>构造 Open Protocol 响应：{长度:D4} + MID + "001" + 填充 + "\x00"。</summary>
    private static string BuildResponse(string mid, int totalChars)
    {
        string head = $"{totalChars:D4}";
        string pad = new string('0', totalChars - head.Length - mid.Length - 3);
        return head + mid + "001" + pad + "\x00";
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }
}
