using System.Threading;
using System.Threading.Tasks;

namespace OperationGuidance_new.Utils {
    public class ChannelMessageQueue<T> : IMessageQueue<T> {
        private readonly System.Threading.Channels.Channel<T> _channel;

        public ChannelMessageQueue() {
            _channel = System.Threading.Channels.Channel.CreateUnbounded<T>(new System.Threading.Channels.UnboundedChannelOptions {
                SingleReader = true,
            });
        }

        public System.Threading.Channels.Channel<T> Channel => _channel;

        public async ValueTask EnqueueAsync(T message, CancellationToken ct = default) {
            await _channel.Writer.WriteAsync(message, ct).ConfigureAwait(false);
        }

        public bool TryComplete() => _channel.Writer.TryComplete();

        public async Task WaitForDrainAsync(CancellationToken ct) {
            await _channel.Reader.Completion.WaitAsync(ct).ConfigureAwait(false);
        }
    }
}
