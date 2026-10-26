using System.Net;
using System.Net.Sockets;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Tests.Tasks;

/// <summary>
/// 模拟速动 X7（55AA 长度前缀帧）。
/// 默认行为：
/// - 锁/解锁（cmd 0x01）只记录时间，不回包（与真机一致）；
/// - PSet（cmd 0x02）：距最近一次结果帧不足 BusyAfterRundownMs 时忽略（模拟控制器忙碌窗口），否则回 PSET_OK。
/// </summary>
public sealed class MockSudongX7Server : IDisposable
{
    public const string StatusFrameHex = SudongTestFrames.Status;
    public const string PSetOkFrameHex = SudongTestFrames.Ack;
    public const string ResultFrameHex = SudongTestFrames.Result;

    private const int MaxFrameBytes = 256;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private NetworkStream? _stream;

    public int Port { get; private set; }
    public int AcceptedConnections { get; private set; }
    public int IgnoredPSetCount { get; private set; }
    public DateTime? LastRundownUtc { get; private set; }
    public DateTime? LastLockUnlockUtc { get; private set; }
    public bool Silent { get; set; }
    public bool CoalesceAckWithStatus { get; set; }
    public int BusyAfterRundownMs { get; set; } = 1000;
    /// <summary>距上一次锁/解锁命令不足该值时忽略 PSet。0 = 不模拟（默认）。该值是推断，不是设备实测规格。</summary>
    public int BusyAfterCommandMs { get; set; } = 0;
    public int InterMessageDelayMs { get; set; } = 20;
    public List<DateTime> AcceptedPSetTimes { get; } = new();

    public MockSudongX7Server() {
        _listener = new TcpListener(IPAddress.Loopback, 0);
    }

    public void Start() {
        _listener.Start();
        Port = ((IPEndPoint) _listener.LocalEndpoint).Port;
        _ = Task.Run(AcceptLoopAsync);
    }

    public async Task PushRundownResultAsync() {
        NetworkStream stream = await WaitForStreamAsync();
        lock (_lock) {
            LastRundownUtc = DateTime.UtcNow;
        }
        await WriteAsync(stream, ResultFrameHex);
    }

    public async Task PushFrameAsync(string hex) {
        NetworkStream stream = await WaitForStreamAsync();
        await WriteAsync(stream, hex);
    }

    /// <summary>等待客户端 stream 就绪。超时直接抛异常，避免测试静默通过。</summary>
    private async Task<NetworkStream> WaitForStreamAsync(int timeoutMs = 2000) {
        await MockPF6000Server.WaitUntilAsync(() => {
            lock (_lock) {
                return _stream != null;
            }
        }, timeoutMs);
        lock (_lock) {
            return _stream!;
        }
    }

    private async Task AcceptLoopAsync() {
        while (!_cts.IsCancellationRequested) {
            TcpClient client;
            try {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
            } catch {
                break;
            }
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client) {
        NetworkStream? stream = null;
        try {
            using (client) {
                stream = client.GetStream();
                lock (_lock) {
                    // _stream 与连接计数一起发布，保证对外可见状态与真实可写状态一致
                    _stream = stream;
                    AcceptedConnections++;
                }
                var buffer = new List<byte>();
                var chunk = new byte[4096];
                while (!_cts.IsCancellationRequested) {
                    int n = await stream.ReadAsync(chunk, _cts.Token);
                    if (n <= 0) break;
                    buffer.AddRange(chunk.Take(n));
                    while (true) {
                        int start = -1;
                        for (int i = 0; i + 1 < buffer.Count; i++) {
                            if (buffer[i] == 0x55 && buffer[i + 1] == 0xAA) {
                                start = i;
                                break;
                            }
                        }
                        if (start < 0) {
                            if (buffer.Count > 1) buffer.RemoveRange(0, buffer.Count - 1);
                            break;
                        }
                        if (start > 0) buffer.RemoveRange(0, start);
                        if (buffer.Count < 3) break;
                        int total = buffer[2] + 4;
                        if (total < 5 || total > MaxFrameBytes) {
                            buffer.RemoveAt(0);
                            continue;
                        }
                        if (buffer.Count < total) break;
                        byte[] frame = buffer.GetRange(0, total).ToArray();
                        buffer.RemoveRange(0, total);
                        await HandleFrameAsync(stream, frame);
                    }
                }
            }
        } catch {
            // 客户端断开或 Dispose 取消，正常退出
        } finally {
            lock (_lock) {
                if (_stream == stream) _stream = null;
            }
        }
    }

    private async Task HandleFrameAsync(NetworkStream stream, byte[] frame) {
        if (frame.Length < 4) return;
        byte cmd = frame[3];
        if (cmd == 0x01) { // 锁 / 解锁
            lock (_lock) {
                LastLockUnlockUtc = DateTime.UtcNow;
            }
            return;
        }
        if (cmd != 0x02) return; // 只处理 PSet

        if (Silent) return;

        DateTime? lastResult;
        DateTime? lastCommand;
        lock (_lock) {
            lastResult = LastRundownUtc;
            lastCommand = LastLockUnlockUtc;
        }
        bool busyByRundown = lastResult.HasValue
            && (DateTime.UtcNow - lastResult.Value).TotalMilliseconds < BusyAfterRundownMs;
        bool busyByCommand = BusyAfterCommandMs > 0
            && lastCommand.HasValue
            && (DateTime.UtcNow - lastCommand.Value).TotalMilliseconds < BusyAfterCommandMs;
        if (busyByRundown || busyByCommand) {
            lock (_lock) {
                IgnoredPSetCount++;
            }
            return;
        }

        lock (_lock) {
            AcceptedPSetTimes.Add(DateTime.UtcNow);
        }
        if (CoalesceAckWithStatus) {
            await WriteRawAsync(stream, StatusFrameHex + PSetOkFrameHex);
        } else {
            await WriteAsync(stream, PSetOkFrameHex);
        }
    }

    private async Task WriteAsync(NetworkStream stream, string hex) {
        await WriteRawAsync(stream, hex);
        await Task.Delay(InterMessageDelayMs, _cts.Token);
    }

    private static async Task WriteRawAsync(NetworkStream stream, string hex) {
        await stream.WriteAsync(MainUtils.ToBytes(hex), CancellationToken.None);
    }

    public void Dispose() {
        _cts.Cancel();
        _listener.Stop();
        _cts.Dispose();
    }
}
